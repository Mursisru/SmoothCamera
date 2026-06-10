using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Precomputed config gates — avoid repeated ini reads and branches in hot paths.</summary>
    internal static class OrbitRuntimeFlags
    {
        internal static bool OrbitPostMotionActive;
        internal static bool OrbitAutoCenterActive;
        internal static bool HeightScaleActive;
        internal static bool DynamicFramingActive;
        internal static bool CombatFollowActive;
        internal static bool HudCompensationActive;
        internal static float HeightScale = 1f;

        internal static void Refresh()
        {
            HeightScale = SmoothCameraConfigCache.OrbitHeightMultiplier;
            HeightScaleActive = Mathf.Abs(HeightScale - 1f) >= 0.001f;
            DynamicFramingActive = SmoothCameraConfigCache.OrbitDynamicFramingEnabled
                && SmoothCameraConfigCache.OrbitDynamicFramingStrength > 0.001f;
            CombatFollowActive = SmoothCameraConfigCache.CombatFollowEnabled;
            OrbitAutoCenterActive = SmoothCameraConfigCache.Orbit.ReturnDelay > 0f;
            HudCompensationActive = SmoothCameraConfigCache.AlignHudToBoresight
                && DynamicFramingActive
                && !CombatFollowActive;

            OrbitPostMotionActive = SmoothCameraConfigCache.Enabled
                && SmoothCameraConfigCache.Orbit.Enabled
                && (CombatFollowActive || DynamicFramingActive || HeightScaleActive);
        }
    }
}
