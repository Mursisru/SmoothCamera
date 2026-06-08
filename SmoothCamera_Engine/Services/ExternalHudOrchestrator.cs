using SmoothCamera_Engine.Config;

namespace SmoothCamera_Engine.Services
{
    internal static class ExternalHudOrchestrator
    {
        internal static void Refresh(CameraStateManager cam)
        {
            if (cam == null || !SmoothCameraConfig.Enabled.Value || !SmoothCameraConfig.ExternalHudEnabled.Value)
                return;

            var mode = CameraStateManager.cameraMode;
            var settings = SmoothCameraConfig.ForMode(mode);
            if (settings == null || !settings.ForceExternalHud.Value)
                return;

            if (!IsExternalFlightMode(mode))
                return;

            Unit following = cam.followingUnit;
            if (following == null || !GameManager.IsLocalAircraft(following))
                return;

            var aircraft = following as Aircraft;
            if (aircraft == null || SceneSingleton<FlightHud>.i == null)
                return;

            SceneSingleton<FlightHud>.i.SetAircraft(aircraft);
            FlightHud.EnableCanvas(true);
            DynamicMap.EnableCanvas(true);
        }

        internal static bool ShouldShowMinimap()
        {
            if (!SmoothCameraConfig.Enabled.Value || !SmoothCameraConfig.ExternalHudEnabled.Value)
                return false;

            var cam = SceneSingleton<CameraStateManager>.i;
            if (cam == null || cam.followingUnit == null)
                return false;

            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return false;

            var mode = CameraStateManager.cameraMode;
            if (!IsExternalFlightMode(mode))
                return false;

            var settings = SmoothCameraConfig.ForMode(mode);
            return settings != null && settings.ForceExternalHud.Value;
        }

        internal static bool IsExternalFlightMode(CameraMode mode)
        {
            return mode == CameraMode.orbit || mode == CameraMode.chase || mode == CameraMode.tv;
        }
    }
}
