using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Pitch framing signal — raw drive, one wideband smooth, then meters.</summary>
    internal static class OrbitFramingSignal
    {
        private const float VerticalOffsetFraction = 0.97f;
        private const float RateReferenceDegPerSec = 48f;
        private const float RateBlendWeight = 1.08f;
        private const float MaxDriveFactor = 3.15f;
        private const float PitchRateDampReference = 72f;
        private const float PitchRateDampMax = 0.42f;
        private const float DriveMobilityFloor = 0.88f;
        private const float CrossingZeroPitchDegrees = 14f;
        private const float CrossingZeroRateDegrees = 18f;

        internal static float ComputeFramingMeters(
            OrbitFramingState state,
            float gunWeight,
            Transform body,
            float angularRateDeg,
            float dt)
        {
            float rawDrive = ComputeRawFramingDrive(state, body, gunWeight);
            float signalHz = OrbitWidebandSmoother.SignalHz(angularRateDeg);
            float drive = OrbitWidebandSmoother.SmoothFloat(
                ref state.SmoothedFramingDrive,
                ref state.FramingDriveInitialized,
                rawDrive,
                signalHz,
                dt);

            state.LastFramingDrive = drive;
            OrbitFramingHelper.PublishDrive(drive);

            if (Mathf.Abs(drive) <= 0.0005f || state.SmoothedOrbitDistance < 0.5f)
                return 0f;

            return -drive * state.SmoothedOrbitDistance * VerticalOffsetFraction;
        }

        private static float ComputeRawFramingDrive(OrbitFramingState state, Transform body, float gunWeight)
        {
            float strength = SmoothCameraConfigCache.OrbitDynamicFramingStrength;
            if (strength <= 0.001f || body == null || !state.PitchInitialized)
                return 0f;

            float pitch = state.PrevPitch;
            float pitchRate = state.LastPitchRate;
            float absPitch = Mathf.Abs(pitch);
            float pitchNorm = Mathf.Clamp01(absPitch / 90f);
            float signedPitchNorm = Mathf.Clamp(pitch / 90f, -1f, 1f);

            float absRate = Mathf.Abs(pitchRate);
            float rateScale = Mathf.Max(
                1f - Mathf.Clamp(absRate / PitchRateDampReference, 0f, PitchRateDampMax),
                DriveMobilityFloor);

            float crossingBlend = 1f;
            if (absRate > CrossingZeroRateDegrees)
            {
                float pitchT = 1f - Mathf.Clamp01(absPitch / CrossingZeroPitchDegrees);
                crossingBlend = Mathf.Lerp(1f, 0.72f, pitchT);
            }

            float attitudeDrive = signedPitchNorm * pitchNorm * strength * rateScale;
            float attitudeScale = 0.45f + 1.05f * pitchNorm;
            float rateNorm = Mathf.Clamp(pitchRate / RateReferenceDegPerSec, -1f, 1f);
            float rateDrive = rateNorm * attitudeScale * strength * rateScale;

            float blendWeight = absRate > 30f
                ? Mathf.Lerp(0.72f, RateBlendWeight, rateScale)
                : RateBlendWeight;
            blendWeight *= crossingBlend;

            float targetDrive = attitudeDrive + rateDrive * blendWeight;
            float maxDrive = MaxDriveFactor * strength;
            targetDrive = Mathf.Clamp(targetDrive, -maxDrive, maxDrive);

            float framingBlend = 1f - Mathf.SmoothStep(0f, 1f, gunWeight) * 0.85f;
            return targetDrive * framingBlend;
        }
    }
}
