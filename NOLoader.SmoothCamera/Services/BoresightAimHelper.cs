using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class BoresightAimHelper
    {
        private const float ReferenceDistance = 1000f;

        private static int _cacheFrame = -1;
        private static Aircraft? _cacheAircraft;
        private static WeaponStation? _cacheStation;
        private static Vector3 _cacheAimDir;

        internal static bool HasActiveWeaponStation(Aircraft aircraft)
        {
            RefreshWeaponCache(aircraft);
            return _cacheStation != null;
        }

        internal static Vector3 GetGunDirectionWorld(Aircraft aircraft)
        {
            if (BoresightLatchHelper.TryGetLatchedAimWorld(aircraft, out Vector3 latched))
                return latched;

            RefreshWeaponCache(aircraft);
            return _cacheAimDir;
        }

        internal static Quaternion ComputeBoresightWorldRotation(Aircraft aircraft, Transform body)
        {
            RefreshWeaponCache(aircraft);
            Vector3 aimDir = _cacheAimDir;
            if (aimDir.sqrMagnitude < 1e-6f)
                aimDir = body.forward;

            aimDir.Normalize();

            float trimPitch = SmoothCameraConfigCache.BoresightPitchTrimDegrees;
            if (Mathf.Abs(trimPitch) > 0.001f)
                aimDir = Quaternion.AngleAxis(-trimPitch, body.right) * aimDir;

            float raiseMeters = SmoothCameraConfigCache.CombatCameraRaiseMeters
                * SmoothCameraConfigCache.GunRaiseMultiplier;
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

        private static void RefreshWeaponCache(Aircraft aircraft)
        {
            if (aircraft == null)
            {
                _cacheStation = null;
                _cacheAimDir = Vector3.forward;
                return;
            }

            var station = aircraft.weaponManager?.currentWeaponStation;
            if (_cacheFrame == Time.frameCount && ReferenceEquals(_cacheAircraft, aircraft) && _cacheStation == station)
                return;

            _cacheFrame = Time.frameCount;
            _cacheAircraft = aircraft;
            _cacheStation = station;

            if (station?.Weapons != null && station.Weapons.Count > 0)
                _cacheAimDir = AverageWeaponForward(station, aircraft.transform.forward);
            else
                _cacheAimDir = aircraft.transform.forward;
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
