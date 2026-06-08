using System.Collections.Generic;
using SmoothCamera_Engine.Config;
using UnityEngine;

namespace SmoothCamera_Engine.Services
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
            PanTiltState state;
            if (!States.TryGetValue(owner, out state))
                return false;
            if (state.IsRecentering)
                return true;
            return Time.unscaledTime < state.RecenterGraceUntil;
        }

        internal static void ProcessAfterInputs(object owner, ModeSmoothSettings settings, ref float panView, ref float tiltView)
        {
            if (settings == null || settings.ReturnDelay.Value <= 0f)
                return;
            if (GameManager.playerInput == null)
                return;

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            PanTiltState state = GetState(owner);
            float axisX = GameManager.playerInput.GetAxis("Pan View");
            float axisY = GameManager.playerInput.GetAxis("Tilt View");
            float inputMagnitudeSqr = axisX * axisX + axisY * axisY;
            if (inputMagnitudeSqr > InputThresholdSqr)
            {
                state.LastInputTime = now;
                state.IsRecentering = false;
                state.RecenterGraceUntil = 0f;
            }

            if (now - state.LastInputTime <= settings.ReturnDelay.Value)
            {
                state.IsRecentering = false;
                return;
            }

            float defaultTilt = settings.DefaultTilt.Value;
            float lerpT = dt * settings.ReturnSpeed.Value;
            panView = Mathf.Lerp(panView, 0f, lerpT);
            tiltView = Mathf.Lerp(tiltView, defaultTilt, lerpT);

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
                state.RecenterGraceUntil = now + SmoothCameraConfig.AutoCenterGraceSeconds.Value;
            }
        }

        internal static void ProcessTvPanTilt(object owner, ModeSmoothSettings settings, ref Vector2 panTiltView, ref Vector2 desiredPanTiltView)
        {
            if (settings == null || settings.ReturnDelay.Value <= 0f)
                return;
            if (GameManager.playerInput == null)
                return;

            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            PanTiltState state = GetState(owner);
            float axisX = GameManager.playerInput.GetAxis("Pan View");
            float axisY = GameManager.playerInput.GetAxis("Tilt View");
            float inputMagnitudeSqr = axisX * axisX + axisY * axisY;
            if (inputMagnitudeSqr > InputThresholdSqr)
            {
                state.LastInputTime = now;
                state.IsRecentering = false;
                state.RecenterGraceUntil = 0f;
            }

            if (now - state.LastInputTime <= settings.ReturnDelay.Value)
            {
                state.IsRecentering = false;
                return;
            }

            float lerpT = dt * settings.ReturnSpeed.Value;
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
                state.RecenterGraceUntil = now + SmoothCameraConfig.AutoCenterGraceSeconds.Value;
            }
        }

        internal static void Clear(object owner)
        {
            States.Remove(owner);
        }

        private static PanTiltState GetState(object owner)
        {
            PanTiltState state;
            if (!States.TryGetValue(owner, out state))
            {
                state = new PanTiltState();
                States[owner] = state;
            }
            return state;
        }
    }
}
