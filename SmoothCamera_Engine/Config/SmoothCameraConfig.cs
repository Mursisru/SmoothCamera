using BepInEx.Configuration;

namespace SmoothCamera_Engine.Config
{
    internal sealed class ModeSmoothSettings
    {
        internal ConfigEntry<bool> Enabled { get; }
        internal ConfigEntry<float> ReturnDelay { get; }
        internal ConfigEntry<float> ReturnSpeed { get; }
        internal ConfigEntry<float> DefaultTilt { get; }
        internal ConfigEntry<bool> ForceExternalHud { get; }

        internal ModeSmoothSettings(ConfigFile config, string section, bool defaultEnabled,
            float returnDelay, float returnSpeed, float defaultTilt, bool forceHud)
        {
            Enabled = config.Bind(section, "Enabled", defaultEnabled, $"Enable mod features for {section}.");
            ReturnDelay = config.Bind(section, "ReturnDelay", returnDelay,
                "Seconds after last pan/tilt input before auto-center (orbit/TV).");
            ReturnSpeed = config.Bind(section, "ReturnSpeed", returnSpeed,
                "Auto-center lerp speed for pan/tilt.");
            DefaultTilt = config.Bind(section, "DefaultTilt", defaultTilt,
                "Default tilt angle in degrees when auto-centering.");
            ForceExternalHud = config.Bind(section, "ForceExternalHud", forceHud,
                "Show full flight HUD when following local aircraft in this mode.");
        }
    }

    internal static class SmoothCameraConfig
    {
        internal static ConfigEntry<bool> Enabled { get; private set; }
        internal static ConfigEntry<bool> ExternalHudEnabled { get; private set; }
        internal static ConfigEntry<bool> CombatFollowEnabled { get; private set; }
        internal static ConfigEntry<float> CombatCameraRaiseMeters { get; private set; }
        internal static ConfigEntry<float> GunRaiseMultiplier { get; private set; }
        internal static ConfigEntry<float> GunBoresightPitchDegrees { get; private set; }
        internal static ConfigEntry<float> BoresightPitchTrimDegrees { get; private set; }
        internal static ConfigEntry<float> CruiseAttitudeFollowRate { get; private set; }
        internal static ConfigEntry<float> GunAttitudeFollowRate { get; private set; }
        internal static ConfigEntry<bool> AlignHudToBoresight { get; private set; }
        internal static ConfigEntry<float> GunWeightBlendRate { get; private set; }
        internal static ConfigEntry<float> CombatReleaseLockoutSeconds { get; private set; }
        internal static ConfigEntry<int> GunEngageStableFrames { get; private set; }
        internal static ConfigEntry<bool> GlocEffectsInExternalViews { get; private set; }
        internal static ConfigEntry<float> ViewSwitchSuppressSeconds { get; private set; }
        internal static ConfigEntry<float> CockpitEntrySuppressSeconds { get; private set; }
        internal static ConfigEntry<float> OrbitHeightMultiplier { get; private set; }
        internal static ConfigEntry<float> OrbitBaseLookDownDegrees { get; private set; }
        internal static ConfigEntry<float> OrbitDynamicFramingStrength { get; private set; }
        internal static ConfigEntry<float> AutoCenterGraceSeconds { get; private set; }
        internal static ModeSmoothSettings Orbit { get; private set; }
        internal static ModeSmoothSettings Chase { get; private set; }
        internal static ModeSmoothSettings Tv { get; private set; }
        internal static ModeSmoothSettings Free { get; private set; }

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Master toggle for Smooth Camera.");
            ExternalHudEnabled = config.Bind("General", "ExternalHudEnabled", true,
                "Enable full HUD (Flight, Combat, MFD, minimap) on local aircraft in external views.");

            CombatFollowEnabled = config.Bind("Combat", "CombatFollowEnabled", true,
                "Enable boresight-style orbit camera (smooth in cruise, stiff with VPU).");
            CombatCameraRaiseMeters = config.Bind("Combat", "CombatCameraRaiseMeters", 0f,
                new ConfigDescription("Optional boresight lift (meters); 0 keeps diamond on crosshair.",
                    new AcceptableValueRange<float>(0f, 2f)));
            GunRaiseMultiplier = config.Bind("Combat", "GunRaiseMultiplier", 1.15f,
                new ConfigDescription("Multiply VPU boresight lift.",
                    new AcceptableValueRange<float>(1f, 3f)));
            GunBoresightPitchDegrees = config.Bind("Combat", "GunBoresightPitchDegrees", 0f,
                new ConfigDescription("(Legacy) use BoresightPitchTrimDegrees; kept for cfg compatibility.",
                    new AcceptableValueRange<float>(0f, 12f)));
            BoresightPitchTrimDegrees = config.Bind("Combat", "BoresightPitchTrimDegrees", 0f,
                new ConfigDescription("Fine pitch trim (deg) after weapon-forward aim; 0 = diamond on crosshair.",
                    new AcceptableValueRange<float>(-5f, 5f)));
            CruiseAttitudeFollowRate = config.Bind("Combat", "CruiseAttitudeFollowRate", 7f,
                new ConfigDescription("Cruise boresight follow speed (lower = softer lag on roll/pitch).",
                    new AcceptableValueRange<float>(1f, 30f)));
            GunAttitudeFollowRate = config.Bind("Combat", "GunAttitudeFollowRate", 42f,
                new ConfigDescription("VPU boresight stiffness (higher = harder lock).",
                    new AcceptableValueRange<float>(4f, 60f)));
            AlignHudToBoresight = config.Bind("Combat", "AlignHudToBoresight", true,
                "Compensate HUD reticle for dynamic camera framing (tracks gun aim on screen).");
            GunWeightBlendRate = config.Bind("Combat", "GunWeightBlendRate", 3.5f,
                new ConfigDescription("VPU on/off crossfade speed (avoids snap when switching weapons).",
                    new AcceptableValueRange<float>(0.5f, 12f)));
            CombatReleaseLockoutSeconds = config.Bind("Combat", "CombatReleaseLockoutSeconds", 0.4f,
                new ConfigDescription("Seconds to block VPU camera re-engage after deselecting gun.",
                    new AcceptableValueRange<float>(0.1f, 1.5f)));
            GunEngageStableFrames = config.Bind("Combat", "GunEngageStableFrames", 4,
                new ConfigDescription("Consecutive frames VPU must stay selected before boresight engages.",
                    new AcceptableValueRange<int>(1, 15)));
            GlocEffectsInExternalViews = config.Bind("Combat", "GlocEffectsInExternalViews", true,
                "Show G-LOC blackout/vignette/desaturation in orbit/chase/TV (same as cockpit).");
            ViewSwitchSuppressSeconds = config.Bind("Combat", "ViewSwitchSuppressSeconds", 0.45f,
                new ConfigDescription("Seconds to skip orbit mod writes after camera view switch.",
                    new AcceptableValueRange<float>(0.1f, 1.5f)));
            CockpitEntrySuppressSeconds = config.Bind("Combat", "CockpitEntrySuppressSeconds", 1f,
                new ConfigDescription("Extra suppress when entering cockpit from external view.",
                    new AcceptableValueRange<float>(0.25f, 2f)));

            OrbitHeightMultiplier = config.Bind("Orbit", "OrbitHeightMultiplier", 0.60f,
                new ConfigDescription("Base orbit vertical offset scale (1.0 = vanilla; lower = flatter).",
                    new AcceptableValueRange<float>(0.35f, 1.35f)));
            OrbitBaseLookDownDegrees = config.Bind("Orbit", "OrbitBaseLookDownDegrees", 9f,
                new ConfigDescription("Base camera look-down (deg) toward pivot; keeps aircraft in frame.",
                    new AcceptableValueRange<float>(0f, 25f)));
            OrbitDynamicFramingStrength = config.Bind("Orbit", "OrbitDynamicFramingStrength", 2f,
                new ConfigDescription("Dynamic camera height by pitch: nose up = cam down, nose down = cam up. No rotation (0=off, 2=default).",
                    new AcceptableValueRange<float>(0f, 5f)));
            AutoCenterGraceSeconds = config.Bind("Orbit", "AutoCenterGraceSeconds", 0.35f,
                new ConfigDescription("Seconds after auto-center before VPU boresight can re-engage.",
                    new AcceptableValueRange<float>(0f, 1.5f)));

            Orbit = new ModeSmoothSettings(config, "Orbit", true, 0.7f, 1.8f, 0f, true);
            Chase = new ModeSmoothSettings(config, "Chase", true, 0f, 0f, 0f, true);
            Tv = new ModeSmoothSettings(config, "TV", true, 1f, 1.2f, 0f, true);
            Free = new ModeSmoothSettings(config, "FreeSpectator", false, 0f, 0f, 0f, false);
        }

        internal static ModeSmoothSettings ForMode(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.orbit: return Orbit;
                case CameraMode.chase: return Chase;
                case CameraMode.tv: return Tv;
                case CameraMode.free:
                case CameraMode.relative:
                    return Free;
                default:
                    return null;
            }
        }

        internal static bool IsModeEnabled(CameraMode mode)
        {
            if (!Enabled.Value)
                return false;
            var settings = ForMode(mode);
            return settings != null && settings.Enabled.Value;
        }
    }
}
