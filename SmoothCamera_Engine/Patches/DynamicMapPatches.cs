using HarmonyLib;
using SmoothCamera_Engine.Services;

namespace SmoothCamera_Engine.Patches
{
    [HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.Minimize))]
    internal static class DynamicMapMinimizePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (ExternalHudOrchestrator.ShouldShowMinimap())
                DynamicMap.EnableCanvas(true);
        }
    }
}
