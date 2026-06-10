using System.Collections.Generic;
using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class AutoCenterController
    {
        private const float InputThreshold = 0.01f;
        private const float InputThresholdSqr = InputThreshold * InputThreshold;
        private const float SnapEpsilon = 0.01f;
        private const float RecenterSnapEpsilon = 0.15f;

        private sealed class PanTiltState
        {
            internal float LastInputTime;
            internal bool IsRecentering;
            internal float RecenterGraceUntil;
        }

        private static readonly Dictionary<object, PanTiltState> States = new Dictionary<object, PanTiltState>(8);

        internal static bool IsRecenteringOrGrace(object owner)
        {
            if (owner == null)
                return false;
            if (!States.TryGetValue(owner, out PanTiltState? state))
                return false;
            if (state!.IsRecentering)
                return true;
            return Time.unscaledTime < state.RecenterGraceUntil;
        }

        internal static bool ShouldTickOrbitAutoCenter(
            object owner,
            ModeSmoothSettings settings,
            float panView,
            float tiltView,
            float axisX,
            float axisY)
        {
            if (settings == null || settings.ReturnDelay <= 0f)
                return false;

            if (axisX * axisX + axisY * axisY > InputThresholdSqr)
                return true;

            float defaultTilt = settings.DefaultTilt;

            if (!States.TryGetValue(owner, out PanTiltState? state))
            {
                return Mathf.Abs(panView) > RecenterSnapEpsilon
                    || Mathf.Abs(tiltView - defaultTilt) > RecenterSnapEpsilon;
            }

            if (Time.unscaledTime - state!.LastInputTime <= settings.ReturnDelay)
                return false;

            if (state.IsRecentering)
                return true;

            return Mathf.Abs(panView) > RecenterSnapEpsilon
                || Mathf.Abs(tiltView - defaultTilt) > RecenterSnapEpsilon;
        }

        internal static void ProcessAfterInputs(
            object owner,
            ModeSmoothSettings settings,
            ref float panView,
            ref float tiltView,
            float axisX,
            float axisY)
        {
            if (settings == null || settings.ReturnDelay <= 0f)
                return;

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            PanTiltState state = GetState(owner);
            float inputMagnitudeSqr = axisX * axisX + axisY * axisY;
            if (inputMagnitudeSqr > InputThresholdSqr)
            {
                state.LastInputTime = now;
                state.IsRecentering = false;
                state.RecenterGraceUntil = 0f;
            }

            if (now - state.LastInputTime <= settings.ReturnDelay)
            {
                state.IsRecentering = false;
                return;
            }

            float defaultTilt = settings.DefaultTilt;
            float smoothT = 1f - Mathf.Exp(-settings.ReturnSpeed * dt);
            panView += (0f - panView) * smoothT;
            tiltView += (defaultTilt - tiltView) * smoothT;

            bool atCenter = Mathf.Abs(panView) < RecenterSnapEpsilon
                && Mathf.Abs(tiltView - defaultTilt) < RecenterSnapEpsilon;
            if (!atCenter)
            {
                state.IsRecentering = true;
                return;
            }

            if (Mathf.Abs(panView) < SnapEpsilon)
                panView = 0f;
            if (Mathf.Abs(tiltView - defaultTilt) < SnapEpsilon)
                tiltView = defaultTilt;

            if (state.IsRecentering)
            {
                state.IsRecentering = false;
                state.RecenterGraceUntil = now + SmoothCameraConfigCache.AutoCenterGraceSeconds;
            }
        }

        internal static void ProcessTvPanTilt(
            object owner,
            ModeSmoothSettings settings,
            ref Vector2 panTiltView,
            ref Vector2 desiredPanTiltView,
            float axisX,
            float axisY)
        {
            if (settings == null || settings.ReturnDelay <= 0f)
                return;

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            PanTiltState state = GetState(owner);
            float inputMagnitudeSqr = axisX * axisX + axisY * axisY;
            if (inputMagnitudeSqr > InputThresholdSqr)
            {
                state.LastInputTime = now;
                state.IsRecentering = false;
                state.RecenterGraceUntil = 0f;
            }

            if (now - state.LastInputTime <= settings.ReturnDelay)
            {
                state.IsRecentering = false;
                return;
            }

            float lerpT = Mathf.Clamp01(dt * settings.ReturnSpeed);
            desiredPanTiltView = Vector2.Lerp(desiredPanTiltView, Vector2.zero, lerpT);
            panTiltView = Vector2.Lerp(panTiltView, Vector2.zero, lerpT);

            if (panTiltView.sqrMagnitude > RecenterSnapEpsilon * RecenterSnapEpsilon)
            {
                state.IsRecentering = true;
                return;
            }

            if (state.IsRecentering)
            {
                state.IsRecentering = false;
                state.RecenterGraceUntil = now + SmoothCameraConfigCache.AutoCenterGraceSeconds;
            }
        }

        internal static void Clear(object owner)
        {
            States.Remove(owner);
        }

        internal static void ClearAll()
        {
            States.Clear();
        }

        private static PanTiltState GetState(object owner)
        {
            if (!States.TryGetValue(owner, out PanTiltState? state))
            {
                state = new PanTiltState();
                States[owner] = state;
            }
            return state!;
        }
    }
}
