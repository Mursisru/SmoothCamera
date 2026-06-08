using HarmonyLib;
using SmoothCamera_Engine.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SmoothCamera_Engine.Patches
{
    /// <summary>
    /// When dynamic orbit framing moves the camera, shift the reticle and snap the
    /// boresight diamond onto it. In level cruise (no dynamic framing) vanilla HUD is kept.
    /// </summary>
    [HarmonyPatch(typeof(HUDBoresightState), nameof(HUDBoresightState.UpdateWeaponDisplay))]
    internal static class HudBoresightReticleCompensationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Aircraft aircraft, Image ___boresight, Image ___targetDesignator)
        {
            var camMgr = SceneSingleton<CameraStateManager>.i;
            if (!HudFramingCompensationHelper.ShouldCompensate(camMgr))
                return;
            if (___boresight == null || ___targetDesignator == null || aircraft == null)
                return;

            Vector3 reticle = HudFramingCompensationHelper.ComputeCompensatedReticleScreen(
                aircraft, camMgr.mainCamera);
            reticle.z = 0f;
            ___targetDesignator.transform.position = reticle;
            ___boresight.transform.position = reticle;
        }
    }
}
