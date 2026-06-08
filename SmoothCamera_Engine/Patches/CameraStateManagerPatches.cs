using HarmonyLib;

using SmoothCamera_Engine.Services;



namespace SmoothCamera_Engine.Patches

{

    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SwitchState))]

    internal static class CameraStateManagerSwitchPatch

    {

        [HarmonyPrefix]

        private static void Prefix(CameraStateManager __instance, CameraBaseState state)

        {

            CameraTransitionService.OnSwitchStatePrefix(__instance, state);

            if (state != __instance.cockpitState)

                OrbitCameraController.PrepareViewSwitch(__instance);

        }



        [HarmonyPostfix]

        private static void Postfix(CameraStateManager __instance)

        {

            CameraTransitionService.OnSwitchStatePostfix(__instance);

        }

    }



    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SetFollowingUnit))]

    internal static class CameraStateManagerFollowingPatch

    {

        [HarmonyPostfix]

        private static void Postfix(CameraStateManager __instance)

        {

            ExternalHudOrchestrator.Refresh(__instance);

        }

    }

}


