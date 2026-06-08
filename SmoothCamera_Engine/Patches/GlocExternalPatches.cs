using System.Reflection;
using HarmonyLib;
using SmoothCamera_Engine.Config;
using SmoothCamera_Engine.Services;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SmoothCamera_Engine.Patches
{
    [HarmonyPatch(typeof(GLOC), nameof(GLOC.SimulateGLOC))]
    internal static class GlocSimulateExternalPatch
    {
        private static readonly FieldInfo BloodPressure = AccessTools.Field(typeof(GLOC), "bloodPressure");
        private static readonly FieldInfo BlackoutImage = AccessTools.Field(typeof(GLOC), "blackoutImage");
        private static readonly FieldInfo ColorAdjustments = AccessTools.Field(typeof(GLOC), "colorAdjustments");
        private static readonly FieldInfo Vignette = AccessTools.Field(typeof(GLOC), "vignette");

        [HarmonyPostfix]
        private static void Postfix(GLOC __instance)
        {
            if (!SmoothCameraConfig.GlocEffectsInExternalViews.Value)
                return;
            if (CameraStateManager.cameraMode == CameraMode.cockpit)
                return;

            var cam = SceneSingleton<CameraStateManager>.i;
            if (cam == null || cam.followingUnit == null || !GameManager.IsLocalAircraft(cam.followingUnit))
                return;
            if (!ExternalHudOrchestrator.IsExternalFlightMode(CameraStateManager.cameraMode))
                return;

            float bp = (float)BloodPressure.GetValue(__instance);
            var blackout = (Image)BlackoutImage.GetValue(__instance);
            var colorAdj = (ColorAdjustments)ColorAdjustments.GetValue(__instance);
            var vignette = (Vignette)Vignette.GetValue(__instance);
            if (blackout == null || colorAdj == null || vignette == null)
                return;

            float fade = (bp - 0.2f) / 0.4f;
            float sat = (bp - 0.3f) / 0.4f;
            blackout.color = Color.Lerp(Color.black, Color.clear, fade);
            colorAdj.saturation.value = Mathf.Lerp(-100f, 0f, sat);
            vignette.intensity.value = Mathf.Lerp(1f, 0.4f, fade);
            AudioMixerVolume.SetMasterAudioFilterStrength(
                Mathf.Lerp(250f, 11000f, Mathf.Clamp01(fade)) + 11000f * Mathf.Clamp01(sat));
        }
    }

    [HarmonyPatch(typeof(GLOC), "GLOC_OnSwitchCamera")]
    internal static class GlocSwitchCameraPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!SmoothCameraConfig.GlocEffectsInExternalViews.Value)
                return true;
            if (CameraStateManager.cameraMode == CameraMode.cockpit)
                return true;

            var cam = SceneSingleton<CameraStateManager>.i;
            if (cam == null || cam.followingUnit == null)
                return true;
            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return true;
            if (!ExternalHudOrchestrator.IsExternalFlightMode(CameraStateManager.cameraMode))
                return true;

            // Keep GLOC visuals when switching to external view as local pilot.
            return false;
        }
    }
}
