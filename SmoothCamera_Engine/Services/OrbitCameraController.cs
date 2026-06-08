using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SmoothCamera_Engine.Config;
using UnityEngine;

namespace SmoothCamera_Engine.Services
{
    internal static class OrbitCameraController
    {
        private sealed class OrbitState
        {
            internal float GunWeight;
            internal bool WasGunSelected;
            internal float ReleaseLockUntil;
            internal int GunStableFrames;
            internal Quaternion SmoothedWorldRotation;
            internal bool RotationInitialized;
            internal bool FramingInitialized;
            internal readonly OrbitFramingState Framing = new OrbitFramingState();
        }

        private static readonly Dictionary<int, OrbitState> States = new Dictionary<int, OrbitState>(4);
        private static readonly FieldInfo OrbitPanView = AccessTools.Field(typeof(CameraOrbitState), "panView");
        private static readonly FieldInfo OrbitTiltView = AccessTools.Field(typeof(CameraOrbitState), "tiltView");

        private const float InputThreshold = 0.02f;
        private const float InputThresholdSqr = InputThreshold * InputThreshold;
        private const float ManualPanEpsilon = 0.35f;
        private const float LookAtTargetGate = 0.05f;

        private static float _suppressUntil;
        private static float _cachedPanView;
        private static float _cachedTiltView;
        private static bool _cachedManualInput;
        private static int _cachedInputFrame = -1;

        internal static void RecordOrbitInput(float panView, float tiltView, float axisX, float axisY)
        {
            _cachedPanView = panView;
            _cachedTiltView = tiltView;
            _cachedManualInput = axisX * axisX + axisY * axisY > InputThresholdSqr;
            _cachedInputFrame = Time.frameCount;
        }

        internal static void PrepareViewSwitch(CameraStateManager cam)
        {
            _suppressUntil = Time.unscaledTime + SmoothCameraConfig.ViewSwitchSuppressSeconds.Value;
            States.Clear();
        }

        internal static void PrepareCockpitTransition(CameraStateManager cam)
        {
            float suppress = Mathf.Max(
                SmoothCameraConfig.ViewSwitchSuppressSeconds.Value,
                SmoothCameraConfig.CockpitEntrySuppressSeconds.Value);
            _suppressUntil = Time.unscaledTime + suppress;
            States.Clear();
        }

        internal static void NeutralizeExternalTransform(CameraStateManager cam)
        {
            if (cam == null || cam.cameraPivot == null)
                return;
            if (cam.transform.parent == cam.cameraPivot)
                cam.transform.localRotation = Quaternion.identity;
        }

        internal static void Reset(Unit unit)
        {
            if (unit == null)
                return;
            States.Remove(unit.GetInstanceID());
        }

        internal static void ApplyPostMotion(CameraStateManager cam, CameraOrbitState orbit, float lookAtTargetLerp)
        {
            if (!CanApply(cam, lookAtTargetLerp))
            {
                OrbitFramingHelper.ClearTrackingState();
                return;
            }

            var aircraft = cam.followingUnit as Aircraft;
            Transform body = cam.followingRB != null ? cam.followingRB.transform : null;
            OrbitState state = aircraft != null ? GetState(aircraft) : null;
            float dt = Time.unscaledDeltaTime;
            bool manualView = IsManualOrbitView(cam, orbit);

            if (body != null && state != null)
            {
                if (!state.FramingInitialized)
                {
                    OrbitFramingHelper.Reset(state.Framing, body);
                    state.FramingInitialized = true;
                }

                if (SmoothCameraConfig.CombatFollowEnabled.Value && !manualView)
                {
                    bool gunSelected = BoresightAimHelper.IsGunSelected(aircraft);
                    UpdateGunTransition(state, gunSelected);

                    bool releaseLocked = Time.unscaledTime < state.ReleaseLockUntil;
                    bool gunReady = gunSelected
                        && !releaseLocked
                        && state.GunStableFrames >= SmoothCameraConfig.GunEngageStableFrames.Value;

                    float targetGunWeight = gunReady ? 1f : 0f;
                    state.GunWeight = Mathf.MoveTowards(
                        state.GunWeight,
                        targetGunWeight,
                        SmoothCameraConfig.GunWeightBlendRate.Value * dt);
                }

                OrbitFramingHelper.UpdateSmooth(state.Framing, body, state.GunWeight, dt);
            }

            ApplyOrbitFraming(cam, state);

            if (manualView)
            {
                SyncRotationFromCamera(cam);
                return;
            }

            if (!SmoothCameraConfig.CombatFollowEnabled.Value)
                return;

            if (aircraft == null || body == null || state == null)
                return;

            Quaternion targetWorld = BoresightAimHelper.ComputeBoresightWorldRotation(aircraft, body);

            if (!state.RotationInitialized)
            {
                state.SmoothedWorldRotation = cam.transform.rotation;
                state.RotationInitialized = true;
            }

            float cruiseRate = SmoothCameraConfig.CruiseAttitudeFollowRate.Value;
            float gunRate = SmoothCameraConfig.GunAttitudeFollowRate.Value;
            float followRate = Mathf.Lerp(cruiseRate, gunRate, state.GunWeight);
            float rotT = 1f - Mathf.Exp(-followRate * dt);

            state.SmoothedWorldRotation = Quaternion.Slerp(state.SmoothedWorldRotation, targetWorld, rotT);
            cam.transform.rotation = state.SmoothedWorldRotation;
        }

        private static void ApplyOrbitFraming(CameraStateManager cam, OrbitState state)
        {
            if (state != null)
                OrbitFramingHelper.ApplyFraming(cam, state.Framing);
            else
                OrbitFramingHelper.ApplyBaseHeightScale(cam);
        }

        private static void SyncRotationFromCamera(CameraStateManager cam)
        {
            if (cam.followingUnit == null || cam.followingRB == null)
                return;
            var state = GetState(cam.followingUnit);
            state.SmoothedWorldRotation = cam.transform.rotation;
            state.RotationInitialized = true;
        }

        private static bool CanApply(CameraStateManager cam, float lookAtTargetLerp)
        {
            if (Time.unscaledTime < _suppressUntil)
                return false;
            if (CameraTransitionService.BlockExternalWrites)
                return false;
            if (!SmoothCameraConfig.Enabled.Value)
                return false;
            if (cam == null || cam.followingRB == null || cam.followingUnit == null || cam.cameraPivot == null)
                return false;
            if (cam.currentState != cam.orbitState)
                return false;
            if (CameraStateManager.cameraMode != CameraMode.orbit)
                return false;
            if (lookAtTargetLerp > LookAtTargetGate)
                return false;
            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return false;
            return true;
        }

        private static bool IsManualOrbitView(CameraStateManager cam, CameraOrbitState orbit)
        {
            if (_cachedInputFrame == Time.frameCount && _cachedManualInput)
                return true;

            if (AutoCenterController.IsRecenteringOrGrace(orbit))
                return true;

            float pan = _cachedInputFrame == Time.frameCount
                ? _cachedPanView
                : (float)OrbitPanView.GetValue(orbit);
            float tilt = _cachedInputFrame == Time.frameCount
                ? _cachedTiltView
                : (float)OrbitTiltView.GetValue(orbit);
            float defaultTilt = SmoothCameraConfig.Orbit.DefaultTilt.Value;
            return Mathf.Abs(pan) > ManualPanEpsilon || Mathf.Abs(tilt - defaultTilt) > ManualPanEpsilon;
        }

        private static void UpdateGunTransition(OrbitState state, bool gunSelected)
        {
            if (gunSelected && !state.WasGunSelected)
                state.GunStableFrames = 0;
            else if (gunSelected)
                state.GunStableFrames++;
            else if (!gunSelected && state.WasGunSelected)
            {
                state.ReleaseLockUntil = Time.unscaledTime + SmoothCameraConfig.CombatReleaseLockoutSeconds.Value;
                state.GunStableFrames = 0;
            }
            else if (!gunSelected)
                state.GunStableFrames = 0;

            state.WasGunSelected = gunSelected;
        }

        private static OrbitState GetState(Unit unit)
        {
            int key = unit.GetInstanceID();
            OrbitState state;
            if (!States.TryGetValue(key, out state))
            {
                state = new OrbitState();
                States[key] = state;
            }
            return state;
        }
    }
}
