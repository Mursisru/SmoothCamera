using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Two wideband exp smoothers — rotation and position. Smoothing beats catch-up.</summary>
    internal static class OrbitWidebandSmoother
    {
        internal static float ExpBlend(float hz, float dt)
            => 1f - Mathf.Exp(-Mathf.Max(0.5f, hz) * OrbitFramingHelper.StableDeltaTime(dt));

        /// <summary>Harder maneuver → slower catch-up (smoothing priority).</summary>
        internal static float StressScale(float angularRateDeg)
        {
            float stress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 110f, angularRateDeg));
            return Mathf.Lerp(1f, 0.48f, stress);
        }

        internal static float RotationHz(float angularRateDeg)
            => SmoothCameraConfigCache.OrbitRotationSmoothHz * StressScale(angularRateDeg);

        internal static float PositionHz(float angularRateDeg)
            => SmoothCameraConfigCache.OrbitPresentSmoothHz * StressScale(angularRateDeg);

        internal static float SignalHz(float angularRateDeg)
            => SmoothCameraConfigCache.OrbitSignalSmoothHz * StressScale(angularRateDeg);

        internal static float SmoothFloat(
            ref float current,
            ref bool initialized,
            float target,
            float hz,
            float dt)
        {
            if (!initialized)
            {
                current = target;
                initialized = true;
                return current;
            }

            float t = ExpBlend(hz, dt);
            current += (target - current) * t;
            return current;
        }

        internal static Vector3 SmoothPosition(
            ref Vector3 current,
            ref bool initialized,
            Vector3 target,
            float hz,
            float dt)
        {
            if (!initialized)
            {
                current = target;
                initialized = true;
                return current;
            }

            float t = ExpBlend(hz, dt);
            current += (target - current) * t;
            return current;
        }

        internal static Quaternion SmoothRotation(
            ref Quaternion current,
            ref bool initialized,
            Quaternion target,
            float hz,
            float dt)
        {
            if (!initialized)
            {
                current = target;
                initialized = true;
                return current;
            }

            float t = ExpBlend(hz, dt);
            current = Quaternion.Slerp(current, target, t);
            return current;
        }
    }
}
