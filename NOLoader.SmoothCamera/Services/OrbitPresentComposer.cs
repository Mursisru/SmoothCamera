using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Final camera present — one rotation smoother + one position smoother.</summary>
    internal static class OrbitPresentComposer
    {
        internal static void SyncFromCamera(OrbitState state, Vector3 position, Quaternion rotation)
        {
            state.SmoothedWorldPosition = position;
            state.PositionInitialized = true;
            state.SmoothedWorldRotation = rotation;
            state.RotationInitialized = true;
        }

        internal static void Present(
            CameraStateManager cam,
            OrbitState state,
            Aircraft aircraft,
            Quaternion targetRotation,
            Vector3 targetPosition,
            float angularRateDeg,
            float dt)
        {
            float rotHz = OrbitWidebandSmoother.RotationHz(angularRateDeg);
            float posHz = OrbitWidebandSmoother.PositionHz(angularRateDeg);

            cam.transform.rotation = OrbitWidebandSmoother.SmoothRotation(
                ref state.SmoothedWorldRotation,
                ref state.RotationInitialized,
                targetRotation,
                rotHz,
                dt);

            cam.transform.position = OrbitWidebandSmoother.SmoothPosition(
                ref state.SmoothedWorldPosition,
                ref state.PositionInitialized,
                targetPosition,
                posHz,
                dt);

            BoresightLatchHelper.UpdateHudLatch(aircraft, cam.transform.rotation);
        }
    }
}
