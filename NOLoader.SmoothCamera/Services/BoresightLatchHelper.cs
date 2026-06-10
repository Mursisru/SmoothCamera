using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>HUD-only aim latch — camera rotation uses direct world slerp in OrbitCameraController.</summary>
    internal static class BoresightLatchHelper
    {
        private static int _latchedAircraftId = -1;
        private static Vector3 _latchedAimWorld = Vector3.forward;

        internal static bool TryGetLatchedAimWorld(Aircraft aircraft, out Vector3 direction)
        {
            if (aircraft != null
                && aircraft.GetInstanceID() == _latchedAircraftId
                && _latchedAimWorld.sqrMagnitude > 1e-6f)
            {
                direction = _latchedAimWorld;
                return true;
            }

            direction = Vector3.forward;
            return false;
        }

        internal static void Invalidate(Aircraft? aircraft)
        {
            if (aircraft == null || aircraft.GetInstanceID() == _latchedAircraftId)
            {
                _latchedAircraftId = -1;
                _latchedAimWorld = Vector3.forward;
            }
        }

        internal static void UpdateHudLatch(Aircraft aircraft, Quaternion smoothedWorldRotation)
        {
            _latchedAimWorld = smoothedWorldRotation * Vector3.forward;
            _latchedAircraftId = aircraft.GetInstanceID();
        }
    }
}
