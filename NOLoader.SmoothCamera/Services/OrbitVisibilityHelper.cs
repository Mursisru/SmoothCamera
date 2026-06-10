using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class OrbitVisibilityHelper
    {
        private const float LookAtTargetGate = 0.05f;
        private const float DeadbandScreenFraction = 0.10f;
        private const float ManeuverPitchRateGate = 18f;
        private const float ManeuverPitchAngleGate = 12f;
        private const float SteepPitchVisibilityCap = 38f;
        private const float OutputEpsilon = 0.0005f;

        internal static void Reset(OrbitState state)
        {
            if (state == null)
                return;
            state.VisibilityTargetOffset = 0f;
            state.VisibilityAppliedOffset = 0f;
        }

        /// <summary>Screen-feedback offset along view up (does not mutate transform).</summary>
        internal static Vector3 ComputeVisibilityOffset(
            CameraStateManager cam,
            Aircraft aircraft,
            OrbitState state,
            float lookAtLerp,
            float pitchRate,
            float pitchDegrees,
            float dt,
            Vector3 virtualCameraPos,
            Quaternion virtualCameraRot)
        {
            if (!OrbitRuntimeFlags.VisibilityFramingActive || state == null)
            {
                DecayOffsets(state, dt);
                return BuildOutput(virtualCameraRot, state);
            }

            if (lookAtLerp > LookAtTargetGate)
            {
                DecayOffsets(state, dt);
                return BuildOutput(virtualCameraRot, state);
            }

            if (cam == null || cam.mainCamera == null || aircraft == null)
            {
                DecayOffsets(state, dt);
                return BuildOutput(virtualCameraRot, state);
            }

            float absPitch = Mathf.Abs(pitchDegrees);
            float absPitchRate = Mathf.Abs(pitchRate);
            bool steepPitch = absPitch >= SteepPitchVisibilityCap;
            bool maneuvering = absPitchRate >= ManeuverPitchRateGate
                || absPitch >= ManeuverPitchAngleGate;

            Camera camera = cam.mainCamera;
            Vector3 focus = cam.followingRB != null
                ? cam.followingRB.worldCenterOfMass
                : aircraft.transform.position;

            Vector3 sp = VirtualWorldToScreen(camera, virtualCameraPos, virtualCameraRot, focus);
            if (sp.z < 0f)
            {
                DecayOffsets(state, dt);
                return BuildOutput(virtualCameraRot, state);
            }

            float screenH = Screen.height;
            if (screenH < 1f)
            {
                DecayOffsets(state, dt);
                return BuildOutput(virtualCameraRot, state);
            }

            float marginBottom = screenH * SmoothCameraConfigCache.OrbitVisibilityMarginBottom;
            float marginTop = screenH * SmoothCameraConfigCache.OrbitVisibilityMarginTop;
            float targetY = screenH * SmoothCameraConfigCache.OrbitVisibilityTargetScreenY;
            float deadband = screenH * DeadbandScreenFraction;

            bool outOfBand = sp.y < marginBottom || sp.y > marginTop;
            bool offTarget = Mathf.Abs(sp.y - targetY) > deadband;
            float targetUpShift = 0f;

            // During maneuvers only keep aircraft on screen (safety); fine Y targeting fights framing.
            if (outOfBand)
            {
                float strength = maneuvering ? 0.45f : 1f;
                targetUpShift = ComputeShiftMeters(
                    camera, virtualCameraPos, focus, screenH, targetY - sp.y, strength, steepPitch);
            }
            else if (!maneuvering && offTarget && !steepPitch)
            {
                targetUpShift = ComputeShiftMeters(
                    camera, virtualCameraPos, focus, screenH, targetY - sp.y, 0.22f, steepPitch);
            }

            float targetRate = SmoothCameraConfigCache.OrbitVisibilityFollowRate;
            float appliedRate = SmoothCameraConfigCache.OrbitVisibilityAppliedFollowRate;

            if (Mathf.Abs(targetUpShift) > OutputEpsilon)
            {
                float maxStep = SmoothCameraConfigCache.OrbitVisibilityMaxStepMeters * Mathf.Max(dt, 1f / 120f) * 60f;
                targetUpShift = Mathf.Clamp(targetUpShift, -maxStep, maxStep);

                float targetT = 1f - Mathf.Exp(-targetRate * dt);
                float targetStep = (targetUpShift - state.VisibilityTargetOffset) * targetT;
                float maxTargetStep = SmoothCameraConfigCache.OrbitVisibilityMaxStepMeters * dt * 60f;
                if (maxTargetStep > 0f)
                    targetStep = Mathf.Clamp(targetStep, -maxTargetStep, maxTargetStep);
                state.VisibilityTargetOffset += targetStep;
            }
            else
            {
                DecayOffsets(state, dt);
            }

            float appliedT = 1f - Mathf.Exp(-appliedRate * dt);
            float appliedStep = (state.VisibilityTargetOffset - state.VisibilityAppliedOffset) * appliedT;
            float maxAppliedStep = SmoothCameraConfigCache.OrbitVisibilityMaxStepMeters * dt * 45f;
            if (maxAppliedStep > 0f)
                appliedStep = Mathf.Clamp(appliedStep, -maxAppliedStep, maxAppliedStep);
            state.VisibilityAppliedOffset += appliedStep;

            float maxShift = SmoothCameraConfigCache.OrbitVisibilityMaxShiftMeters;
            if (steepPitch)
                maxShift *= 0.35f;
            if (maneuvering)
                maxShift *= 0.5f;
            state.VisibilityAppliedOffset = Mathf.Clamp(state.VisibilityAppliedOffset, -maxShift, maxShift);

            return BuildOutput(virtualCameraRot, state);
        }

        private static Vector3 BuildOutput(Quaternion virtualCameraRot, OrbitState? state)
        {
            if (state == null || Mathf.Abs(state.VisibilityAppliedOffset) <= OutputEpsilon)
                return Vector3.zero;
            return virtualCameraRot * Vector3.up * state.VisibilityAppliedOffset;
        }

        private static Vector3 VirtualWorldToScreen(
            Camera camera,
            Vector3 cameraWorldPos,
            Quaternion cameraWorldRot,
            Vector3 worldPos)
        {
            Matrix4x4 worldToCamera = Matrix4x4.TRS(cameraWorldPos, cameraWorldRot, Vector3.one).inverse;
            Vector3 camSpace = worldToCamera.MultiplyPoint3x4(worldPos);
            Vector4 clip = camera.projectionMatrix * new Vector4(camSpace.x, camSpace.y, camSpace.z, 1f);
            if (clip.w <= 0f)
                return new Vector3(0f, 0f, -1f);

            Vector3 ndc = new Vector3(clip.x, clip.y, clip.z) / clip.w;
            float screenW = Screen.width;
            float screenH = Screen.height;
            return new Vector3(
                (ndc.x + 1f) * 0.5f * screenW,
                (ndc.y + 1f) * 0.5f * screenH,
                clip.w);
        }

        private static float ComputeShiftMeters(
            Camera camera,
            Vector3 cameraPos,
            Vector3 focus,
            float screenH,
            float pixelError,
            float strength,
            bool steepPitch)
        {
            float dist = Mathf.Max((focus - cameraPos).magnitude, 8f);
            float tanHalf = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float metersPerPixel = 2f * dist * tanHalf / screenH;
            float sensitivity = SmoothCameraConfigCache.OrbitVisibilitySensitivity * strength;
            if (steepPitch)
                sensitivity *= 0.2f;
            return pixelError * metersPerPixel * sensitivity;
        }

        private static void DecayOffsets(OrbitState state, float dt)
        {
            if (state == null)
                return;

            float targetRate = SmoothCameraConfigCache.OrbitVisibilityFollowRate * 1.2f;
            float appliedRate = SmoothCameraConfigCache.OrbitVisibilityAppliedFollowRate * 1.2f;
            float targetT = 1f - Mathf.Exp(-targetRate * dt);
            float appliedT = 1f - Mathf.Exp(-appliedRate * dt);

            float targetStep = -state.VisibilityTargetOffset * targetT;
            float maxTargetStep = SmoothCameraConfigCache.OrbitVisibilityMaxStepMeters * dt * 60f;
            if (maxTargetStep > 0f)
                targetStep = Mathf.Clamp(targetStep, -maxTargetStep, maxTargetStep);
            state.VisibilityTargetOffset += targetStep;

            float appliedStep = (state.VisibilityTargetOffset - state.VisibilityAppliedOffset) * appliedT;
            float maxAppliedStep = SmoothCameraConfigCache.OrbitVisibilityMaxStepMeters * dt * 45f;
            if (maxAppliedStep > 0f)
                appliedStep = Mathf.Clamp(appliedStep, -maxAppliedStep, maxAppliedStep);
            state.VisibilityAppliedOffset += appliedStep;
        }
    }
}

