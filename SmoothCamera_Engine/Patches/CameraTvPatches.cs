using HarmonyLib;

using SmoothCamera_Engine.Config;

using SmoothCamera_Engine.Services;

using UnityEngine;



namespace SmoothCamera_Engine.Patches

{

    [HarmonyPatch(typeof(CameraTVState), nameof(CameraTVState.UpdateState))]

    internal static class CameraTvUpdatePatch

    {

        [HarmonyPostfix]

        private static void Postfix(CameraTVState __instance, CameraStateManager cam,

            ref Vector2 ___panTiltView, ref Vector2 ___desiredPanTiltView)

        {

            if (!CameraTransitionService.ShouldApplyExternalWrite(cam, __instance))

                return;

            if (!SmoothCameraConfig.IsModeEnabled(CameraMode.tv))

                return;



            AutoCenterController.ProcessTvPanTilt(__instance, SmoothCameraConfig.Tv, ref ___panTiltView, ref ___desiredPanTiltView);

        }

    }


    [HarmonyPatch(typeof(CameraTVState), nameof(CameraTVState.LeaveState))]

    internal static class CameraTvLeavePatch

    {

        [HarmonyPostfix]

        private static void Postfix(CameraTVState __instance, CameraStateManager cam)

        {

            AutoCenterController.Clear(__instance);

        }

    }

}


