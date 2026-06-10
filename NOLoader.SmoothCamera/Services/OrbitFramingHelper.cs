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
        internal float SmoothedFramingDrive;
        internal bool FramingDriveInitialized;
        internal float LastFramingDrive;
    }

    internal static class OrbitFramingHelper
    {
        private const float PitchRateFilterHz = 16f;
        private const float MaxIntegratorDeltaTime = 1f / 45f;

        internal static float MeasurePitchDegrees(Transform body)
            => Mathf.Asin(Mathf.Clamp(body.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        internal static float StableDeltaTime(float dt)
            => Mathf.Clamp(dt, 1e-4f, MaxIntegratorDeltaTime);

        internal static float MeasureDeltaTime(float dt)
            => Mathf.Max(dt, 1e-4f);

        internal static void RefreshPitchRate(OrbitFramingState state, Transform body, float dt)
        {
            if (body == null)
                return;

            float measureDt = MeasureDeltaTime(dt);
            float pitch = MeasurePitchDegrees(body);
            if (!state.PitchInitialized)
            {
                state.PrevPitch = pitch;
                state.PitchInitialized = true;
                state.LastPitchRate = 0f;
                state.SmoothedPitchRate = 0f;
                return;
            }

            float rawRate = (pitch - state.PrevPitch) / measureDt;
            float filterT = 1f - Mathf.Exp(-PitchRateFilterHz * measureDt);
            state.SmoothedPitchRate += (rawRate - state.SmoothedPitchRate) * filterT;
            state.LastPitchRate = state.SmoothedPitchRate;
            state.PrevPitch = pitch;
        }

        internal static void RefreshOrbitDistance(
            OrbitFramingState state,
            CameraStateManager cam,
            Vector3 vanillaOrbitPos,
            float dt)
        {
            if (cam == null || cam.cameraPivot == null)
                return;

            // Instant — 5Hz smooth here caused ~0.2s periodic catch-up jerks during maneuvers.
            state.SmoothedOrbitDistance = (vanillaOrbitPos - cam.cameraPivot.position).magnitude;
            state.OrbitDistanceInitialized = true;
        }

        internal static void Reset(OrbitFramingState state, Transform body)
        {
            state.PrevPitch = body != null ? MeasurePitchDegrees(body) : 0f;
            state.PitchInitialized = body != null;
            state.LastPitchRate = 0f;
            state.SmoothedPitchRate = 0f;
            state.SmoothedOrbitDistance = 0f;
            state.OrbitDistanceInitialized = false;
            state.SmoothedFramingDrive = 0f;
            state.FramingDriveInitialized = false;
            state.LastFramingDrive = 0f;
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

        internal static void PublishFramingOffset(Vector3 worldOffset)
            => LastDynamicFramingOffset = worldOffset;

        internal static void PublishDrive(float drive)
            => LastSmoothedDrive = drive;

        internal static void ClearTrackingState()
        {
            LastDynamicFramingOffset = Vector3.zero;
            LastSmoothedDrive = 0f;
        }

        internal static Vector3 LastDynamicFramingOffset { get; private set; }
        internal static float LastSmoothedDrive { get; private set; }
    }
}
