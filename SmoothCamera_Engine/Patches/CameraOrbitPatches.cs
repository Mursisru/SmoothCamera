using HarmonyLib;
using SmoothCamera_Engine.Config;
using SmoothCamera_Engine.Services;
using UnityEngine;

namespace SmoothCamera_Engine.Patches
{
    [HarmonyPatch(typeof(CameraOrbitState), "CameraMotion")]
    internal static class CameraOrbitMotionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CameraStateManager cam, CameraOrbitState __instance, float ___lookAtTargetLerp)
        {
            if (!CameraTransitionService.ShouldApplyExternalWrite(cam, __instance))
                return;

            OrbitCameraController.ApplyPostMotion(cam, __instance, ___lookAtTargetLerp);
        }
    }

    [HarmonyPatch(typeof(CameraOrbitState), "Inputs")]
    internal static class CameraOrbitInputsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CameraOrbitState __instance, CameraStateManager cam, ref float ___panView, ref float ___tiltView)
        {
            if (!CameraTransitionService.ShouldApplyExternalWrite(cam, __instance))
                return;
            if (!SmoothCameraConfig.IsModeEnabled(CameraMode.orbit))
                return;

            float axisX = 0f;
            float axisY = 0f;
            if (GameManager.playerInput != null && !Cursor.visible)
            {
                axisX = GameManager.playerInput.GetAxis("Pan View");
                axisY = GameManager.playerInput.GetAxis("Tilt View");
            }

            OrbitCameraController.RecordOrbitInput(___panView, ___tiltView, axisX, axisY);
            AutoCenterController.ProcessAfterInputs(__instance, SmoothCameraConfig.Orbit, ref ___panView, ref ___tiltView);
        }
    }

    [HarmonyPatch(typeof(CameraOrbitState), nameof(CameraOrbitState.EnterState))]
    internal static class CameraOrbitEnterPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CameraStateManager cam)
        {
            OrbitCameraController.NeutralizeExternalTransform(cam);
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }
    }

    [HarmonyPatch(typeof(CameraOrbitState), nameof(CameraOrbitState.LeaveState))]
    internal static class CameraOrbitLeavePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CameraOrbitState __instance, CameraStateManager cam)
        {
            OrbitCameraController.NeutralizeExternalTransform(cam);
            AutoCenterController.Clear(__instance);
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }
    }
}
