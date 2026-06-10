using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Combat boresight frame state — prefix sets, postfix consumes.</summary>
    internal static class OrbitCombatRotationHelper
    {
        internal static bool CombatBoresightFrameActive;

        private static float _lastDebugLogTime = -1f;
        private static string _lastDebugReason = string.Empty;

        internal static void ResetFrame()
        {
            CombatBoresightFrameActive = false;
        }

        internal static void PreparePrefix(CameraOrbitState orbit, CameraStateManager cam)
        {
            var aircraft = cam?.followingUnit as Aircraft;
            CombatBoresightFrameActive = OrbitRuntimeFlags.CombatFollowActive
                && !OrbitCameraController.ShouldBlockCombatBoresight(orbit, aircraft);
        }

        internal static void LogBoresightSkip(string reason)
        {
            if (!SmoothCameraConfigCache.DebugCameraGates)
                return;
            if (reason == _lastDebugReason && Time.unscaledTime - _lastDebugLogTime < 1f)
                return;

            _lastDebugReason = reason;
            _lastDebugLogTime = Time.unscaledTime;
            Debug.Log("[SmoothCamera] boresight skip: " + reason);
        }
    }
}
