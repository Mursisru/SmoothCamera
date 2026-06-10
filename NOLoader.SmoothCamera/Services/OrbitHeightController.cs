using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Builds target world position; presentation smoothing is in OrbitPresentComposer.</summary>
    internal static class OrbitHeightController
    {
        internal static void Reset(OrbitState state)
        {
            state.PositionInitialized = false;
        }

        internal static Vector3 ComputeTargetPosition(
            CameraStateManager cam,
            OrbitFramingState framing,
            Transform body,
            Vector3 vanillaOrbitPos,
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
                framingMeters = OrbitFramingSignal.ComputeFramingMeters(
                    framing, gunWeight, body, angularRateDeg, dt);

            OrbitFramingHelper.PublishFramingOffset(Vector3.up * framingMeters);
            return vanillaOrbitPos + Vector3.up * (baseMeters + framingMeters);
        }
    }
}
