using NOLoader.ModConfig;
using NOLoader.SmoothCamera.Services;

namespace NOLoader.SmoothCamera
{
    internal sealed class ModeSmoothSettings
    {
        internal bool Enabled;
        internal float ReturnDelay;
        internal float ReturnSpeed;
        internal float DefaultTilt;
        internal bool ForceExternalHud;
    }

    internal static class SmoothCameraConfigCache
    {
        internal static bool Enabled = true;
        internal static bool ExternalHudEnabled = true;
        internal static bool DebugCameraGates;
        internal static bool CombatFollowEnabled = true;
        internal static float CombatCameraRaiseMeters;
        internal static float GunRaiseMultiplier = 1.15f;
        internal static float GunBoresightPitchDegrees;
        internal static float BoresightPitchTrimDegrees;
        internal static float CruiseAttitudeFollowRate = 7f;
        internal static float GunAttitudeFollowRate = 42f;
        internal static bool AlignHudToBoresight = true;
        internal static float GunWeightBlendRate = 3.5f;
        internal static float CombatReleaseLockoutSeconds = 0.4f;
        internal static int GunEngageStableFrames = 4;
        internal static bool GlocEffectsInExternalViews = true;
        internal static float ViewSwitchSuppressSeconds = 0.45f;
        internal static float CockpitEntrySuppressSeconds = 1f;
        internal static float OrbitHeightMultiplier = 0.68f;
        internal static float OrbitBaseLookDownDegrees = 9f;
        internal static float OrbitDynamicFramingStrength = 2f;
        internal static float AutoCenterGraceSeconds = 0.35f;
        internal static bool OrbitDynamicFramingEnabled = true;
        internal static float OrbitPitchFramingStartDegrees = 5f;
        internal static float OrbitHeightDropPerPitchDegree = 0.026f;
        internal static float OrbitHeightDropPerRollDegree = 0.004f;
        internal static float OrbitLookDownPerPitchUpDegree = 0.72f;
        internal static float OrbitLookDownPerPitchDownDegree = 0.35f;
        internal static float OrbitHorizPullBackAtMaxPitch = 0.22f;
        internal static float OrbitPitchFrameBlendStrength = 0.82f;
        internal static float OrbitMinHeightMultiplier = 0.38f;
        internal static float OrbitMaxHeightMultiplier = 0.88f;
        internal static float OrbitFramingFollowRate = 9f;
        internal static float OrbitAppliedFramingFollowRate = 2.2f;
        internal static float OrbitCameraPosSmoothRate = 8.5f;
        internal static float OrbitCameraPosMaxMetersPerSec = 11f;
        internal static float OrbitFramingMaxDriveStep = 2.2f;
        internal static float OrbitCameraRotSmoothRate = 9f;
        internal static bool OrbitVisibilityFramingEnabled = true;
        internal static float OrbitVisibilityTargetScreenY = 0.42f;
        internal static float OrbitVisibilityMarginBottom = 0.12f;
        internal static float OrbitVisibilityMarginTop = 0.82f;
        internal static float OrbitVisibilityFollowRate = 2.2f;
        internal static float OrbitVisibilityAppliedFollowRate = 0.9f;
        internal static float OrbitVisibilityMaxShiftMeters = 6f;
        internal static float OrbitVisibilityMaxStepMeters = 0.22f;
        internal static float OrbitVisibilitySensitivity = 0.22f;
        internal static ModeSmoothSettings Orbit = new ModeSmoothSettings
        {
            Enabled = true,
            ReturnDelay = 0.7f,
            ReturnSpeed = 1.8f,
            ForceExternalHud = true
        };
        internal static ModeSmoothSettings Chase = new ModeSmoothSettings
        {
            Enabled = true,
            ForceExternalHud = true
        };
        internal static ModeSmoothSettings Tv = new ModeSmoothSettings
        {
            Enabled = true,
            ReturnDelay = 1f,
            ReturnSpeed = 1.2f,
            ForceExternalHud = true
        };
        internal static ModeSmoothSettings Free = new ModeSmoothSettings();

        internal static void Load(ModIniConfig cfg)
        {
            Enabled = cfg.GetBool("General", "Enabled", true);
            ExternalHudEnabled = cfg.GetBool("General", "ExternalHudEnabled", true);
            DebugCameraGates = cfg.GetBool("General", "DebugCameraGates", false);

            CombatFollowEnabled = cfg.GetBool("Combat", "CombatFollowEnabled", true);
            CombatCameraRaiseMeters = cfg.GetFloat("Combat", "CombatCameraRaiseMeters", 0f);
            GunRaiseMultiplier = cfg.GetFloat("Combat", "GunRaiseMultiplier", 1.15f);
            GunBoresightPitchDegrees = cfg.GetFloat("Combat", "GunBoresightPitchDegrees", 0f);
            BoresightPitchTrimDegrees = cfg.GetFloat("Combat", "BoresightPitchTrimDegrees", 0f);
            CruiseAttitudeFollowRate = cfg.GetFloat("Combat", "CruiseAttitudeFollowRate", 7f);
            GunAttitudeFollowRate = cfg.GetFloat("Combat", "GunAttitudeFollowRate", 42f);
            AlignHudToBoresight = cfg.GetBool("Combat", "AlignHudToBoresight", true);
            GunWeightBlendRate = cfg.GetFloat("Combat", "GunWeightBlendRate", 3.5f);
            CombatReleaseLockoutSeconds = cfg.GetFloat("Combat", "CombatReleaseLockoutSeconds", 0.4f);
            GunEngageStableFrames = cfg.GetInt("Combat", "GunEngageStableFrames", 4);
            GlocEffectsInExternalViews = cfg.GetBool("Combat", "GlocEffectsInExternalViews", true);
            ViewSwitchSuppressSeconds = cfg.GetFloat("Combat", "ViewSwitchSuppressSeconds", 0.45f);
            CockpitEntrySuppressSeconds = cfg.GetFloat("Combat", "CockpitEntrySuppressSeconds", 1f);

            OrbitHeightMultiplier = cfg.GetFloat("Orbit", "OrbitHeightMultiplier", 0.68f);
            OrbitBaseLookDownDegrees = cfg.GetFloat("Orbit", "OrbitBaseLookDownDegrees", 9f);
            OrbitDynamicFramingStrength = cfg.GetFloat("Orbit", "OrbitDynamicFramingStrength", 2f);
            AutoCenterGraceSeconds = cfg.GetFloat("Orbit", "AutoCenterGraceSeconds", 0.35f);
            OrbitDynamicFramingEnabled = cfg.GetBool("Orbit", "OrbitDynamicFramingEnabled", true);
            OrbitPitchFramingStartDegrees = cfg.GetFloat("Orbit", "OrbitPitchFramingStartDegrees", 5f);
            OrbitHeightDropPerPitchDegree = cfg.GetFloat("Orbit", "OrbitHeightDropPerPitchDegree", 0.016f);
            OrbitHeightDropPerRollDegree = cfg.GetFloat("Orbit", "OrbitHeightDropPerRollDegree", 0.004f);
            OrbitLookDownPerPitchUpDegree = cfg.GetFloat("Orbit", "OrbitLookDownPerPitchUpDegree", 0.72f);
            OrbitLookDownPerPitchDownDegree = cfg.GetFloat("Orbit", "OrbitLookDownPerPitchDownDegree", 0.35f);
            OrbitHorizPullBackAtMaxPitch = cfg.GetFloat("Orbit", "OrbitHorizPullBackAtMaxPitch", 0.22f);
            OrbitPitchFrameBlendStrength = cfg.GetFloat("Orbit", "OrbitPitchFrameBlendStrength", 0.82f);
            OrbitMinHeightMultiplier = cfg.GetFloat("Orbit", "OrbitMinHeightMultiplier", 0.38f);
            OrbitMaxHeightMultiplier = cfg.GetFloat("Orbit", "OrbitMaxHeightMultiplier", 0.88f);
            OrbitFramingFollowRate = cfg.GetFloat("Orbit", "OrbitFramingFollowRate", 9f);
            OrbitAppliedFramingFollowRate = cfg.GetFloat("Orbit", "OrbitAppliedFramingFollowRate", 2.2f);
            OrbitCameraPosSmoothRate = cfg.GetFloat("Orbit", "OrbitCameraPosSmoothRate", 8.5f);
            OrbitCameraPosMaxMetersPerSec = cfg.GetFloat("Orbit", "OrbitCameraPosMaxMetersPerSec", 11f);
            OrbitFramingMaxDriveStep = cfg.GetFloat("Orbit", "OrbitFramingMaxDriveStep", 2.2f);
            OrbitCameraRotSmoothRate = cfg.GetFloat("Orbit", "OrbitCameraRotSmoothRate", 9f);
            OrbitVisibilityFramingEnabled = cfg.GetBool("Orbit", "OrbitVisibilityFramingEnabled", true);
            OrbitVisibilityTargetScreenY = cfg.GetFloat("Orbit", "OrbitVisibilityTargetScreenY", 0.42f);
            OrbitVisibilityMarginBottom = cfg.GetFloat("Orbit", "OrbitVisibilityMarginBottom", 0.12f);
            OrbitVisibilityMarginTop = cfg.GetFloat("Orbit", "OrbitVisibilityMarginTop", 0.82f);
            OrbitVisibilityFollowRate = cfg.GetFloat("Orbit", "OrbitVisibilityFollowRate", 2.2f);
            OrbitVisibilityAppliedFollowRate = cfg.GetFloat("Orbit", "OrbitVisibilityAppliedFollowRate", 0.9f);
            OrbitVisibilityMaxShiftMeters = cfg.GetFloat("Orbit", "OrbitVisibilityMaxShiftMeters", 6f);
            OrbitVisibilityMaxStepMeters = cfg.GetFloat("Orbit", "OrbitVisibilityMaxStepMeters", 0.22f);
            OrbitVisibilitySensitivity = cfg.GetFloat("Orbit", "OrbitVisibilitySensitivity", 0.22f);

            LoadMode(cfg, "Orbit", Orbit, true, 0.7f, 1.8f, 0f, true);
            LoadMode(cfg, "Chase", Chase, true, 0f, 0f, 0f, true);
            LoadMode(cfg, "TV", Tv, true, 1f, 1.2f, 0f, true);
            LoadMode(cfg, "FreeSpectator", Free, false, 0f, 0f, 0f, false);

            OrbitFramingHelper.RefreshConfigCache();
            OrbitRuntimeFlags.Refresh();
        }

        private static void LoadMode(
            ModIniConfig cfg,
            string section,
            ModeSmoothSettings mode,
            bool defaultEnabled,
            float returnDelay,
            float returnSpeed,
            float defaultTilt,
            bool forceHud)
        {
            mode.Enabled = cfg.GetBool(section, "Enabled", defaultEnabled);
            mode.ReturnDelay = cfg.GetFloat(section, "ReturnDelay", returnDelay);
            mode.ReturnSpeed = cfg.GetFloat(section, "ReturnSpeed", returnSpeed);
            mode.DefaultTilt = cfg.GetFloat(section, "DefaultTilt", defaultTilt);
            mode.ForceExternalHud = cfg.GetBool(section, "ForceExternalHud", forceHud);
        }

        internal static ModeSmoothSettings? ForMode(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.orbit:
                    return Orbit;
                case CameraMode.chase:
                    return Chase;
                case CameraMode.tv:
                    return Tv;
                case CameraMode.free:
                case CameraMode.relative:
                    return Free;
                default:
                    return null;
            }
        }

        internal static bool IsModeEnabled(CameraMode mode)
        {
            if (!Enabled)
                return false;
            var settings = ForMode(mode);
            return settings != null && settings.Enabled;
        }
    }
}
