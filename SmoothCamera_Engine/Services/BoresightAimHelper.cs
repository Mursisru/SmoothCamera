using SmoothCamera_Engine.Config;
using UnityEngine;

namespace SmoothCamera_Engine.Services
{
    internal static class BoresightAimHelper
    {
        private const float ReferenceDistance = 1000f;

        internal static bool IsGunSelected(Aircraft aircraft)
        {
            var station = aircraft?.weaponManager?.currentWeaponStation;
            return station?.WeaponInfo != null && station.WeaponInfo.gun;
        }

        internal static Vector3 GetGunDirectionWorld(Aircraft aircraft)
        {
            if (aircraft == null)
                return Vector3.forward;

            var station = aircraft.weaponManager?.currentWeaponStation;
            if (station != null && station.WeaponInfo != null && station.WeaponInfo.gun)
                return AverageWeaponForward(station, aircraft.transform.forward);

            return aircraft.transform.forward;
        }

        internal static Quaternion ComputeBoresightWorldRotation(Aircraft aircraft, Transform body)
        {
            Vector3 aimDir = GetGunDirectionWorld(aircraft);
            if (aimDir.sqrMagnitude < 1e-6f)
                aimDir = body.forward;

            aimDir.Normalize();

            float trimPitch = SmoothCameraConfig.BoresightPitchTrimDegrees.Value;
            if (Mathf.Abs(trimPitch) > 0.001f)
                aimDir = Quaternion.AngleAxis(-trimPitch, body.right) * aimDir;

            float raiseMeters = SmoothCameraConfig.CombatCameraRaiseMeters.Value
                * SmoothCameraConfig.GunRaiseMultiplier.Value;
            if (raiseMeters > 0.001f)
            {
                float raisePitch = Mathf.Rad2Deg * Mathf.Atan2(raiseMeters, ReferenceDistance);
                aimDir = Quaternion.AngleAxis(-raisePitch, body.right) * aimDir;
            }

            Vector3 up = body.up;
            if (Vector3.Cross(aimDir, up).sqrMagnitude < 1e-6f)
                up = Vector3.up;

            return Quaternion.LookRotation(aimDir, up);
        }

        private static Vector3 AverageWeaponForward(WeaponStation station, Vector3 fallback)
        {
            if (station?.Weapons == null || station.Weapons.Count == 0)
                return fallback;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Weapon weapon in station.Weapons)
            {
                if (weapon == null)
                    continue;
                sum += weapon.transform.forward;
                count++;
            }

            return count > 0 ? sum.normalized : fallback;
        }
    }
}
