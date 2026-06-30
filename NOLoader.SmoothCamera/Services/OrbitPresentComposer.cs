using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Final camera rotation — one wideband smoother. Position stays on vanilla orbit.</summary>
    internal static class OrbitPresentComposer
    {
        internal static void SyncFromCamera(OrbitState state, Quaternion rotation)
        {
            state.SmoothedWorldRotation = rotation;
            state.RotationInitialized = true;
        }

        internal static void PresentRotation(
            CameraStateManager cam,
            OrbitState state,
            Aircraft aircraft,
            Quaternion targetRotation,
            float angularRateDeg,
            float dt)
        {
            float rotHz = OrbitWidebandSmoother.RotationHz(angularRateDeg);

            cam.transform.rotation = OrbitWidebandSmoother.SmoothRotation(
                ref state.SmoothedWorldRotation,
                ref state.RotationInitialized,
                targetRotation,
                rotHz,
                dt);

            BoresightLatchHelper.UpdateHudLatch(aircraft, cam.transform.rotation);
        }
    }
}
