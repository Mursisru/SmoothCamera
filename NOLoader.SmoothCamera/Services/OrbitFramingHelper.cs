using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal sealed class OrbitFramingState
    {
        internal float PrevPitch;
        internal bool PitchInitialized;
        internal float LastPitchRate;
        internal float SmoothedPitchRate;
        internal float SmoothedOrbitDistance;
        internal bool OrbitDistanceInitialized;
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
        private const float ManeuverRateReference = 55f;
        private const float ManeuverMinScale = 0.25f;
        private const float CruiseAppliedRateScale = 0.65f;
        private const float PitchRateFilterHz = 14f;
        private const float OrbitDistanceSmoothHz = 7f;
        private const float MaxSmoothDeltaTime = 1f / 45f;

        private static float _targetFollowRateCached = 9f;
        private static float _appliedFollowRateCached = 1.5f;

        internal static float MeasurePitchDegrees(Transform body)
            => Mathf.Asin(Mathf.Clamp(body.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        internal static void RefreshConfigCache()
        {
            _targetFollowRateCached = SmoothCameraConfigCache.OrbitFramingFollowRate;
            _appliedFollowRateCached = SmoothCameraConfigCache.OrbitAppliedFramingFollowRate;
        }

        internal static float StableDeltaTime(float dt)
            => Mathf.Clamp(dt, 1e-4f, MaxSmoothDeltaTime);

        internal static void RefreshPitchRate(OrbitFramingState state, Transform body, float dt)
        {
            if (body == null)
                return;

            float stableDt = StableDeltaTime(dt);
            float pitch = MeasurePitchDegrees(body);
            if (!state.PitchInitialized)
            {
                state.PrevPitch = pitch;
                state.PitchInitialized = true;
                state.LastPitchRate = 0f;
                state.SmoothedPitchRate = 0f;
                return;
            }

            float rawRate = (pitch - state.PrevPitch) / stableDt;
            float filterT = 1f - Mathf.Exp(-PitchRateFilterHz * stableDt);
            state.SmoothedPitchRate += (rawRate - state.SmoothedPitchRate) * filterT;
            state.LastPitchRate = state.SmoothedPitchRate;
            state.PrevPitch = pitch;
        }

        internal static void RefreshOrbitDistance(OrbitFramingState state, CameraStateManager cam, float dt)
        {
            if (cam == null || cam.cameraPivot == null)
                return;

            float rawDist = (cam.transform.position - cam.cameraPivot.position).magnitude;
            if (!state.OrbitDistanceInitialized)
            {
                state.SmoothedOrbitDistance = rawDist;
                state.OrbitDistanceInitialized = true;
                return;
            }

            float t = 1f - Mathf.Exp(-OrbitDistanceSmoothHz * StableDeltaTime(dt));
            state.SmoothedOrbitDistance = Mathf.Lerp(state.SmoothedOrbitDistance, rawDist, t);
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

            float pitch = state.PrevPitch;
            float pitchRate = state.LastPitchRate;

            float absPitch = Mathf.Abs(pitch);
            float pitchNorm = Mathf.Clamp01(absPitch / 90f);
            float signedPitchNorm = Mathf.Clamp(pitch / 90f, -1f, 1f);

            float absRate = Mathf.Abs(pitchRate);
            float rateScale = 1f - Mathf.Clamp(absRate / PitchRateDampReference, 0f, PitchRateDampMax);
            bool crossingZero = absPitch < 14f && absRate > 18f;

            float attitudeDrive = signedPitchNorm * pitchNorm * strength * rateScale;
            float attitudeScale = 0.3f + 0.7f * pitchNorm;
            float rateNorm = Mathf.Clamp(pitchRate / RateReferenceDegPerSec, -1.25f, 1.25f);
            float rateDrive = rateNorm * attitudeScale * strength * rateScale;

            float blendWeight = absRate > 30f
                ? Mathf.Lerp(0.4f, RateBlendWeight, rateScale)
                : RateBlendWeight;
            if (crossingZero)
                blendWeight *= 0.45f;

            float targetDrive = attitudeDrive + rateDrive * blendWeight;
            float maxDrive = MaxDriveFactor * strength;
            targetDrive = Mathf.Clamp(targetDrive, -maxDrive, maxDrive);

            float framingBlend = 1f - Mathf.SmoothStep(0f, 1f, gunWeight) * 0.85f;
            targetDrive *= framingBlend;

            float targetRate = _targetFollowRateCached * rateScale;

            float appliedRate = _appliedFollowRateCached;
            if (crossingZero)
                appliedRate *= 0.55f;
            else if (absRate > 22f)
                appliedRate = Mathf.Min(Mathf.Max(appliedRate * 1.25f, 4f), 5.5f);
            else
                appliedRate *= rateScale;

            if (gunWeight < 0.3f && absRate <= 22f && !crossingZero)
                appliedRate *= CruiseAppliedRateScale;

            float stableDt = StableDeltaTime(dt);
            float targetT = 1f - Mathf.Exp(-targetRate * stableDt);
            state.SmoothedTargetDrive += (targetDrive - state.SmoothedTargetDrive) * targetT;

            float appliedT = 1f - Mathf.Exp(-appliedRate * stableDt);
            float driveStep = (state.SmoothedTargetDrive - state.SmoothedAppliedDrive) * appliedT;
            float maxDriveStep = SmoothCameraConfigCache.OrbitFramingMaxDriveStep * stableDt;
            if (maxDriveStep > 0f)
                driveStep = Mathf.Clamp(driveStep, -maxDriveStep, maxDriveStep);
            state.SmoothedAppliedDrive += driveStep;
        }

        internal static float ComputeManeuverSmoothScale(OrbitFramingState state)
        {
            float absRate = Mathf.Abs(state.LastPitchRate);
            return Mathf.Lerp(1f, ManeuverMinScale, Mathf.Clamp01(absRate / ManeuverRateReference));
        }

        internal static void Reset(OrbitFramingState state, Transform body)
        {
            state.PrevPitch = body != null ? MeasurePitchDegrees(body) : 0f;
            state.PitchInitialized = body != null;
            state.LastPitchRate = 0f;
            state.SmoothedPitchRate = 0f;
            state.SmoothedOrbitDistance = 0f;
            state.OrbitDistanceInitialized = false;
            state.SmoothedTargetDrive = 0f;
            state.SmoothedAppliedDrive = 0f;
        }

        /// <summary>Dynamic pitch framing along view up (meters).</summary>
        internal static float ComputeDynamicFramingMeters(OrbitFramingState? state, bool needDynamic)
        {
            LastSmoothedDrive = state != null ? state.SmoothedAppliedDrive : 0f;
            if (!needDynamic || state == null || Mathf.Abs(state.SmoothedAppliedDrive) <= 0.0005f)
                return 0f;
            if (state.SmoothedOrbitDistance < 0.5f)
                return 0f;

            float maneuverScale = ComputeManeuverSmoothScale(state);
            return -state.SmoothedAppliedDrive * state.SmoothedOrbitDistance * VerticalOffsetFraction * maneuverScale;
        }

        internal static Vector3 ComputeDynamicFramingOffset(Vector3 viewUp, OrbitFramingState? state, bool needDynamic)
        {
            float meters = ComputeDynamicFramingMeters(state, needDynamic);
            Vector3 dynamic = meters * viewUp;
            LastDynamicFramingOffset = dynamic;
            return dynamic;
        }

        /// <summary>Framing offset from vanilla orbit position (does not mutate transform).</summary>
        internal static Vector3 ComputeFramingOffset(CameraStateManager cam, OrbitFramingState? state, bool needDynamic, Vector3 viewUp)
            => ComputeBaseHeightOffset(cam) + ComputeDynamicFramingOffset(viewUp, state, needDynamic);

        internal static void ApplyFraming(CameraStateManager cam, OrbitFramingState? state)
        {
            if (cam == null)
                return;
            Vector3 viewUp = cam.transform.rotation * Vector3.up;
            cam.transform.position += ComputeFramingOffset(cam, state, state != null, viewUp);
        }

        internal static Vector3 ComputeBaseHeightOffset(CameraStateManager cam)
        {
            if (cam == null || cam.cameraPivot == null || !OrbitRuntimeFlags.HeightScaleActive)
                return Vector3.zero;

            Vector3 pivotPos = cam.cameraPivot.position;
            Vector3 offset = cam.transform.position - pivotPos;
            Vector3 upPart = Vector3.Project(offset, Vector3.up);
            Vector3 rest = offset - upPart;
            Vector3 scaled = pivotPos + rest + upPart * OrbitRuntimeFlags.HeightScale;
            return scaled - cam.transform.position;
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
            if (cam == null)
                return;
            cam.transform.position += ComputeBaseHeightOffset(cam);
        }
    }
}
