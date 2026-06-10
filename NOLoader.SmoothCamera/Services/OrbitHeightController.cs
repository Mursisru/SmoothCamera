using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>
    /// World-Y scalar offset on vanilla orbit position. No Vector3 smooth — no lateral leakage.
    /// </summary>
    internal static class OrbitHeightController
    {
        private const float VanillaSnapThresholdMeters = 2.5f;
        private const float SnapCatchUpRateMul = 3f;

        internal static void Reset(OrbitState state)
        {
            state.SmoothedVerticalMeters = 0f;
            state.HeightOffsetInitialized = false;
            state.LastVanillaOrbitPos = Vector3.zero;
        }

        internal static float ComputeTargetVerticalMeters(
            CameraStateManager cam,
            OrbitFramingState framing,
            Transform body,
            bool needFraming,
            float gunWeight,
            float dt)
        {
            float baseMeters = 0f;
            if (OrbitRuntimeFlags.HeightScaleActive)
                baseMeters = Vector3.Dot(OrbitFramingHelper.ComputeBaseHeightOffset(cam), Vector3.up);

            float framingMeters = 0f;
            if (needFraming && body != null && framing.PitchInitialized)
                framingMeters = OrbitFramingSignal.ComputeFramingMeters(framing, gunWeight, body, dt);

            OrbitFramingHelper.PublishFramingOffset(Vector3.up * framingMeters);
            return baseMeters + framingMeters;
        }

        internal static void Apply(
            CameraStateManager cam,
            OrbitState state,
            Vector3 vanillaOrbitPos,
            float targetMeters,
            float pitchRateDeg,
            float dt)
        {
            if (!state.HeightOffsetInitialized)
            {
                state.SmoothedVerticalMeters = targetMeters;
                state.HeightOffsetInitialized = true;
                state.LastVanillaOrbitPos = vanillaOrbitPos;
                cam.transform.position = vanillaOrbitPos + Vector3.up * state.SmoothedVerticalMeters;
                return;
            }

            float snapDelta = (vanillaOrbitPos - state.LastVanillaOrbitPos).magnitude;
            float rate = Mathf.Max(0.5f, SmoothCameraConfigCache.OrbitVerticalFollowRate);
            rate *= ComputeManeuverFollowBoost(pitchRateDeg);

            if (snapDelta > VanillaSnapThresholdMeters)
                rate *= SnapCatchUpRateMul;

            float stableDt = OrbitFramingHelper.StableDeltaTime(dt);
            float t = 1f - Mathf.Exp(-rate * stableDt);
            state.SmoothedVerticalMeters += (targetMeters - state.SmoothedVerticalMeters) * t;

            state.LastVanillaOrbitPos = vanillaOrbitPos;
            cam.transform.position = vanillaOrbitPos + Vector3.up * state.SmoothedVerticalMeters;
        }

        private static float ComputeManeuverFollowBoost(float absPitchRateDeg)
        {
            return Mathf.Lerp(1f, 2.4f, Mathf.Clamp01(absPitchRateDeg / 40f));
        }
    }
}
