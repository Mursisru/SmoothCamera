namespace NOLoader.SmoothCamera.Services
{
    /// <summary>
    /// Ensures mod camera writes never run after a view switch within the same patched method.
    /// </summary>
    internal static class CameraTransitionService
    {
        internal static bool BlockExternalWrites { get; private set; }

        internal static void ReleaseWriteBlock()
        {
            BlockExternalWrites = false;
        }

        internal static void ResetAll()
        {
            BlockExternalWrites = false;
        }

        internal static bool IsExternalState(CameraStateManager cam, CameraBaseState state)
        {
            if (cam == null || state == null)
                return false;
            return state == cam.orbitState || state == cam.chaseState || state == cam.TVState;
        }

        internal static bool IsActiveExternal(CameraStateManager cam)
        {
            return cam != null && IsExternalState(cam, cam.currentState);
        }

        internal static void OnSwitchStatePrefix(CameraStateManager cam, CameraBaseState nextState)
        {
            BlockExternalWrites = false;
            if (cam == null || nextState == null)
                return;

            if (nextState == cam.cockpitState)
                PrepareCockpitEntry(cam);
            else if (IsExternalState(cam, cam.currentState))
                CleanupExternalCamera(cam);
        }

        internal static void OnSwitchStatePostfix(CameraStateManager cam)
        {
            ReleaseWriteBlock();
            ExternalHudOrchestrator.Refresh(cam);
        }

        internal static void PrepareCockpitEntry(CameraStateManager cam)
        {
            BlockExternalWrites = true;
            OrbitCameraController.PrepareCockpitTransition(cam);
            CleanupExternalCamera(cam);
        }

        internal static void CleanupExternalCamera(CameraStateManager cam)
        {
            if (cam == null)
                return;

            OrbitCameraController.NeutralizeExternalTransform(cam);
        }

        internal static bool ShouldApplyExternalWrite(CameraStateManager cam, CameraBaseState expectedState)
        {
            if (BlockExternalWrites)
                return false;
            if (!SmoothCameraConfigCache.Enabled)
                return false;
            if (cam == null || expectedState == null)
                return false;
            return cam.currentState == expectedState;
        }
    }
}
