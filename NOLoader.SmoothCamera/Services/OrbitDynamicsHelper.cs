using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class OrbitDynamicsHelper
    {
        internal static float GetAngularRateDeg(Rigidbody? rb, float pitchRateDeg)
        {
            float bodyRate = rb != null ? rb.angularVelocity.magnitude * Mathf.Rad2Deg : 0f;
            return Mathf.Max(bodyRate, Mathf.Abs(pitchRateDeg));
        }
    }
}
