namespace NOLoader.SmoothCamera.Services
{
    internal static class ExternalHudOrchestrator
    {
        internal static void Refresh(CameraStateManager cam)
        {
            if (cam == null || !SmoothCameraConfigCache.Enabled || !SmoothCameraConfigCache.ExternalHudEnabled)
                return;

            var mode = CameraStateManager.cameraMode;
            var settings = SmoothCameraConfigCache.ForMode(mode);
            if (settings == null || !settings.ForceExternalHud)
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
            if (!SmoothCameraConfigCache.Enabled || !SmoothCameraConfigCache.ExternalHudEnabled)
                return false;

            var cam = SceneSingleton<CameraStateManager>.i;
            if (cam == null || cam.followingUnit == null)
                return false;

            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return false;

            var mode = CameraStateManager.cameraMode;
            if (!IsExternalFlightMode(mode))
                return false;

            var settings = SmoothCameraConfigCache.ForMode(mode);
            return settings != null && settings.ForceExternalHud;
        }

        internal static bool IsExternalFlightMode(CameraMode mode)
        {
            return mode == CameraMode.orbit || mode == CameraMode.chase || mode == CameraMode.tv;
        }
    }
}
