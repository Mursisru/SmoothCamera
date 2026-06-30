using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>
    /// Scalar world-Y offset on instant vanilla orbit — no full-position smooth (no longitudinal lag).
    /// </summary>
    internal static class OrbitHeightController
    {
        internal static void Reset(OrbitState state)
        {
            state.SmoothedVerticalMeters = 0f;
            state.HeightOffsetInitialized = false;
        }

        internal static float ComputeTargetVerticalMeters(
            CameraStateManager cam,
            OrbitFramingState framing,
            Transform body,
            bool needFraming,
            float gunWeight,
            float angularRateDeg,
            float dt)
        {
            float baseMeters = 0f;
            if (OrbitRuntimeFlags.HeightScaleActive)
                baseMeters = Vector3.Dot(OrbitFramingHelper.ComputeBaseHeightOffset(cam), Vector3.up);

            float framingMeters = 0f;
            if (needFraming && body != null && framing.PitchInitialized)
            {
                framingMeters = OrbitFramingSignal.ComputeFramingMeters(
                    framing, gunWeight, body, angularRateDeg, dt);
            }

            OrbitFramingHelper.PublishFramingOffset(Vector3.up * framingMeters);
            return baseMeters + framingMeters;
        }

        internal static void Apply(
            CameraStateManager cam,
            OrbitState state,
            Vector3 vanillaOrbitPos,
            float targetMeters,
            float angularRateDeg,
            float dt)
        {
            float offsetHz = OrbitWidebandSmoother.PositionHz(angularRateDeg);
            float smoothedMeters = OrbitWidebandSmoother.SmoothFloat(
                ref state.SmoothedVerticalMeters,
                ref state.HeightOffsetInitialized,
                targetMeters,
                offsetHz,
                dt);

            cam.transform.position = vanillaOrbitPos + Vector3.up * smoothedMeters;
        }
    }
}
