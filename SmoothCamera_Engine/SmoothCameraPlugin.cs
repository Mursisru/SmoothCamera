using BepInEx;
using HarmonyLib;
using SmoothCamera_Engine.Config;

namespace SmoothCamera_Engine
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SmoothCameraPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.at747.smoothcamera";
        public const string PluginName = "Smooth Camera AutoCenter";
        public const string PluginVersion = AppVersion.ReleaseBase;

        private Harmony _harmony;

        private void Awake()
        {
            SmoothCameraConfig.Bind(Config);
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SmoothCameraPlugin).Assembly);
            Logger.LogInfo($"{PluginName} {AppVersion.Display} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
