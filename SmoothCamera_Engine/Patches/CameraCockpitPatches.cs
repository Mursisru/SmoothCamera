using System.Reflection;
using HarmonyLib;
using SmoothCamera_Engine.Services;
using UnityEngine;

namespace SmoothCamera_Engine.Patches
{
    [HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.EnterState))]
    internal static class CameraCockpitEnterPatch
    {
        private static readonly FieldInfo CamRelativePos = AccessTools.Field(typeof(CameraCockpitState), "camRelativePos");
        private static readonly FieldInfo CamRelativeVel = AccessTools.Field(typeof(CameraCockpitState), "camRelativeVel");
        private static readonly FieldInfo AntiSlump = AccessTools.Field(typeof(CameraCockpitState), "antiSlump");

        [HarmonyPrefix]
        private static void Prefix(CameraCockpitState __instance)
        {
            CamRelativePos.SetValue(__instance, Vector3.zero);
            CamRelativeVel.SetValue(__instance, Vector3.zero);
            AntiSlump.SetValue(__instance, 0f);
        }
    }

    [HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.LeaveState))]
    internal static class CameraCockpitLeavePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CameraStateManager cam)
        {
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }
    }
}
