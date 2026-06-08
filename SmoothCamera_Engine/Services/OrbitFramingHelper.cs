using SmoothCamera_Engine.Config;
using UnityEngine;

namespace SmoothCamera_Engine.Services
{
    internal sealed class OrbitFramingState
    {
        internal float PrevPitch;
        internal bool PitchInitialized;
        /// <summary>Signed compensation: + = nose up (cam down), - = nose down.</summary>
        internal float SmoothedDrive;
    }

    internal static class OrbitFramingHelper
    {
        private const float FollowRate = 12f;
        private const float VerticalOffsetFraction = 0.78f;
        private const float RateReferenceDegPerSec = 40f;
        private const float RateBlendWeight = 0.72f;
        private const float MaxDriveFactor = 1.65f;

        internal static float MeasurePitchDegrees(Transform body)
        {
            return Mathf.Asin(Mathf.Clamp(body.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        internal static void UpdateSmooth(OrbitFramingState state, Transform body, float gunWeight, float dt)
        {
            float strength = SmoothCameraConfig.OrbitDynamicFramingStrength.Value;

            if (strength <= 0.001f)
            {
                state.SmoothedDrive = 0f;
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

            float targetDrive = attitudeDrive + rateDrive * RateBlendWeight;
            float maxDrive = MaxDriveFactor * strength;
            targetDrive = Mathf.Clamp(targetDrive, -maxDrive, maxDrive);

            float gunPreserve = Mathf.Clamp01(gunWeight);
            targetDrive *= 1f - gunPreserve * 0.85f;

            float t = 1f - Mathf.Exp(-FollowRate * dt);
            state.SmoothedDrive = Mathf.Lerp(state.SmoothedDrive, targetDrive, t);
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
            state.SmoothedDrive = 0f;
        }

        internal static void ApplyFraming(CameraStateManager cam, OrbitFramingState state)
        {
            LastDynamicFramingOffset = Vector3.zero;
            LastSmoothedDrive = state != null ? state.SmoothedDrive : 0f;
            if (cam == null || cam.cameraPivot == null || state == null)
                return;

            ApplyBaseHeightScale(cam);

            float drive = state.SmoothedDrive;
            if (Mathf.Abs(drive) <= 0.0005f)
                return;

            Vector3 beforeDynamic = cam.transform.position;
            Vector3 pivotPos = cam.cameraPivot.position;
            Vector3 offset = cam.transform.position - pivotPos;
            float dist = offset.magnitude;
            if (dist >= 0.5f)
            {
                float verticalShift = -drive * dist * VerticalOffsetFraction;
                cam.transform.position += Vector3.up * verticalShift;
            }

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
            float heightScale = SmoothCameraConfig.OrbitHeightMultiplier.Value;
            if (Mathf.Abs(heightScale - 1f) < 0.001f)
                return;

            Vector3 pivotPos = cam.cameraPivot.position;
            Vector3 offset = cam.transform.position - pivotPos;
            Vector3 upPart = Vector3.Project(offset, Vector3.up);
            Vector3 rest = offset - upPart;
            cam.transform.position = pivotPos + rest + upPart * heightScale;
        }
    }
}
