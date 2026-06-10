using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>
    /// Boresight rotation blended with vanilla orbit rotation in cruise; stiff at VPU.
    /// </summary>
    internal static class OrbitRotationComposer
    {
        private const float VpuStiffGunWeightThreshold = 0.5f;
        private const float CruiseBoresightInfluence = 0.22f;

        internal static void SyncFromCamera(OrbitState state, Quaternion rotation)
        {
            state.SmoothedWorldRotation = rotation;
            state.RotationInitialized = true;
        }

        internal static void Tick(
            CameraStateManager cam,
            OrbitState state,
            Aircraft aircraft,
            Transform body,
            Quaternion vanillaRotation,
            float absPitchRateDeg,
            float dt,
            bool combatFollowActive)
        {
            if (!combatFollowActive)
                return;

            if (!state.RotationInitialized)
            {
                state.SmoothedWorldRotation = vanillaRotation;
                state.RotationInitialized = true;
            }

            Quaternion boresightTarget = BoresightAimHelper.ComputeBoresightWorldRotation(
                aircraft,
                body,
                useLatch: false);

            if (OrbitRuntimeFlags.DiagnosticInstantBoresight)
            {
                state.SmoothedWorldRotation = boresightTarget;
            }
            else if (state.GunWeight >= VpuStiffGunWeightThreshold)
            {
                float followRate = Mathf.Lerp(
                    SmoothCameraConfigCache.CruiseAttitudeFollowRate,
                    SmoothCameraConfigCache.GunAttitudeFollowRate,
                    state.GunWeight);
                float rotT = 1f - Mathf.Exp(-followRate * dt);
                state.SmoothedWorldRotation = Quaternion.Slerp(
                    state.SmoothedWorldRotation,
                    boresightTarget,
                    rotT);
            }
            else
            {
                float influence = Mathf.Lerp(
                    CruiseBoresightInfluence,
                    0.65f,
                    Mathf.SmoothStep(0f, 1f, state.GunWeight / VpuStiffGunWeightThreshold));
                Quaternion cruiseTarget = Quaternion.Slerp(vanillaRotation, boresightTarget, influence);

                float followRate = SmoothCameraConfigCache.CruiseAttitudeFollowRate;
                followRate *= ComputeManeuverRotBoost(absPitchRateDeg, state.SmoothedWorldRotation, cruiseTarget);

                float rotT = 1f - Mathf.Exp(-followRate * dt);
                state.SmoothedWorldRotation = Quaternion.Slerp(
                    state.SmoothedWorldRotation,
                    cruiseTarget,
                    rotT);
            }

            cam.transform.rotation = state.SmoothedWorldRotation;
            BoresightLatchHelper.UpdateHudLatch(aircraft, state.SmoothedWorldRotation);
        }

        private static float ComputeManeuverRotBoost(
            float absPitchRateDeg,
            Quaternion current,
            Quaternion target)
        {
            float rateBoost = Mathf.Lerp(1f, 3.2f, Mathf.Clamp01(absPitchRateDeg / 38f));
            float angleErr = Quaternion.Angle(current, target);
            float errBoost = Mathf.Lerp(1f, 2.5f, Mathf.Clamp01(angleErr / 14f));
            return rateBoost * errBoost;
        }
    }
}
