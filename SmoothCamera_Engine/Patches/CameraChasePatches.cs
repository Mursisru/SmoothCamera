using HarmonyLib;
using SmoothCamera_Engine.Config;

namespace SmoothCamera_Engine.Patches
{
    [HarmonyPatch(typeof(CameraChaseState), nameof(CameraChaseState.CheckHUD))]
    internal static class CameraChaseCheckHudPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref bool ___showHUD)
        {
            if (!SmoothCameraConfig.Enabled.Value || !SmoothCameraConfig.ExternalHudEnabled.Value)
                return;
            if (!SmoothCameraConfig.Chase.ForceExternalHud.Value)
                return;

            var cam = SceneSingleton<CameraStateManager>.i;
            if (cam == null || cam.followingUnit == null || !GameManager.IsLocalAircraft(cam.followingUnit))
                return;

            ___showHUD = true;
        }
    }
}
