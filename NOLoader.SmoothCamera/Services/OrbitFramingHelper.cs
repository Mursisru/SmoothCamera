using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal sealed class OrbitFramingState
    {
        internal float PrevPitch;
        internal bool PitchInitialized;
        internal float SmoothedTargetDrive;
        internal float SmoothedAppliedDrive;
    }

    internal static class OrbitFramingHelper
    {
        private const float VerticalOffsetFraction = 0.78f;
        private const float RateReferenceDegPerSec = 40f;
        private const float RateBlendWeight = 0.72f;
        private const float MaxDriveFactor = 1.65f;
        private const float PitchRateDampReference = 55f;
        private const float PitchRateDampMax = 0.85f;
        private const float CruiseAppliedRateScale = 0.65f;

        private static float _targetFollowRateCached = 9f;
        private static float _appliedFollowRateCached = 1.5f;

        internal static float MeasurePitchDegrees(Transform body)
            => Mathf.Asin(Mathf.Clamp(body.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        internal static void RefreshConfigCache()
        {
            _targetFollowRateCached = SmoothCameraConfigCache.OrbitFramingFollowRate;
            _appliedFollowRateCached = SmoothCameraConfigCache.OrbitAppliedFramingFollowRate;
        }

        internal static void UpdateSmooth(OrbitFramingState state, Transform body, float gunWeight, float dt)
        {
            float strength = SmoothCameraConfigCache.OrbitDynamicFramingStrength;
            if (strength <= 0.001f || body == null)
            {
                state.SmoothedTargetDrive = 0f;
                state.SmoothedAppliedDrive = 0f;
                return;
            }

            float pitch = MeasurePitchDegrees(body);
            if (!state.PitchInitialized)
            {
                state.PrevPitch = pitch;
                state.PitchInitialized = true;
            }

            float pitchRate = dt > 1e-5f ? (pitch - state.PrevPitch) / dt : 0f;
            state.PrevPitch = pitch;

            float absPitch = Mathf.Abs(pitch);
            float pitchNorm = Mathf.Clamp01(absPitch / 90f);
            float pitchSign = ResolveSign(pitch, pitchRate);

            float attitudeDrive = pitchNorm * pitchSign * strength;
            float attitudeScale = 0.3f + 0.7f * pitchNorm;
            float rateNorm = Mathf.Clamp(Mathf.Abs(pitchRate) / RateReferenceDegPerSec, 0f, 1.25f);
            float rateSign = Mathf.Abs(pitchRate) > 1f ? Mathf.Sign(pitchRate) : pitchSign;
            float rateDrive = rateNorm * rateSign * attitudeScale * strength;

            float absRate = Mathf.Abs(pitchRate);
            float rateScale = 1f - Mathf.Clamp(absRate / PitchRateDampReference, 0f, PitchRateDampMax);
            rateDrive *= rateScale;

            float targetDrive = attitudeDrive + rateDrive * RateBlendWeight;
            float maxDrive = MaxDriveFactor * strength;
            targetDrive = Mathf.Clamp(targetDrive, -maxDrive, maxDrive);

            float framingBlend = 1f - Mathf.SmoothStep(0f, 1f, gunWeight) * 0.85f;
            targetDrive *= framingBlend;

            float targetRate = _targetFollowRateCached;
            targetRate *= rateScale;

            float appliedRate = _appliedFollowRateCached;
            appliedRate *= rateScale;
            if (gunWeight < 0.3f)
                appliedRate *= CruiseAppliedRateScale;

            float targetT = 1f - Mathf.Exp(-targetRate * dt);
            state.SmoothedTargetDrive += (targetDrive - state.SmoothedTargetDrive) * targetT;

            float appliedT = 1f - Mathf.Exp(-appliedRate * dt);
            state.SmoothedAppliedDrive += (state.SmoothedTargetDrive - state.SmoothedAppliedDrive) * appliedT;
        }

        private static float ResolveSign(float pitch, float pitchRate)
        {
            if (Mathf.Abs(pitch) >= 1f)
                return Mathf.Sign(pitch);
            if (Mathf.Abs(pitchRate) >= 3f)
                return Mathf.Sign(pitchRate);
            if (Mathf.Abs(pitch) >= 0.05f)
                return Mathf.Sign(pitch);
            return 0f;
        }

        internal static void Reset(OrbitFramingState state, Transform body)
        {
            state.PrevPitch = body != null ? MeasurePitchDegrees(body) : 0f;
            state.PitchInitialized = body != null;
            state.SmoothedTargetDrive = 0f;
            state.SmoothedAppliedDrive = 0f;
        }

        internal static void ApplyFraming(CameraStateManager cam, OrbitFramingState? state)
        {
            LastDynamicFramingOffset = Vector3.zero;
            LastSmoothedDrive = state != null ? state.SmoothedAppliedDrive : 0f;
            if (cam == null || cam.cameraPivot == null)
                return;

            ApplyBaseHeightScale(cam);
            if (state == null || Mathf.Abs(state.SmoothedAppliedDrive) <= 0.0005f)
                return;

            Vector3 beforeDynamic = cam.transform.position;
            Vector3 pivotPos = cam.cameraPivot.position;
            Vector3 offset = cam.transform.position - pivotPos;
            float dist = offset.magnitude;
            if (dist < 0.5f)
                return;

            float verticalShift = -state.SmoothedAppliedDrive * dist * VerticalOffsetFraction;
            cam.transform.position += Vector3.up * verticalShift;
            LastDynamicFramingOffset = cam.transform.position - beforeDynamic;
        }

        internal static void ClearTrackingState()
        {
            LastDynamicFramingOffset = Vector3.zero;
            LastSmoothedDrive = 0f;
        }

        internal static Vector3 LastDynamicFramingOffset { get; private set; }
        internal static float LastSmoothedDrive { get; private set; }

        internal static void ApplyBaseHeightScale(CameraStateManager cam)
        {
            if (cam == null || cam.cameraPivot == null || !OrbitRuntimeFlags.HeightScaleActive)
                return;

            Vector3 pivotPos = cam.cameraPivot.position;
            Vector3 offset = cam.transform.position - pivotPos;
            Vector3 upPart = Vector3.Project(offset, Vector3.up);
            Vector3 rest = offset - upPart;
            cam.transform.position = pivotPos + rest + upPart * OrbitRuntimeFlags.HeightScale;
        }
    }
}
