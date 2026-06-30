using System.Collections.Generic;
using NOLoader.SmoothCamera;
using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class OrbitCameraController
    {
        private static readonly Dictionary<int, OrbitState> States = new Dictionary<int, OrbitState>(4);

        private const float ManualPanEpsilon = 0.35f;
        private const float LookAtTargetGate = 0.05f;

        private static float _suppressUntil;

        internal static void RecordOrbitInput(float panView, float tiltView, float axisX, float axisY)
        {
            OrbitInputCache.Capture(Time.frameCount, panView, tiltView, axisX, axisY);
        }

        internal static void ProcessOrbitMotionPrefix(
            CameraOrbitState orbit,
            CameraStateManager cam,
            ref float panView,
            ref float tiltView,
            float axisX,
            float axisY)
        {
            OrbitCombatRotationHelper.ResetFrame();
            RecordOrbitInput(panView, tiltView, axisX, axisY);

            if (OrbitRuntimeFlags.OrbitAutoCenterActive)
            {
                if (AutoCenterController.ShouldTickOrbitAutoCenter(
                        orbit,
                        SmoothCameraConfigCache.Orbit,
                        panView,
                        tiltView,
                        axisX,
                        axisY))
                {
                    AutoCenterController.ProcessAfterInputs(
                        orbit,
                        SmoothCameraConfigCache.Orbit,
                        ref panView,
                        ref tiltView,
                        axisX,
                        axisY);
                }
            }

            OrbitCombatRotationHelper.PreparePrefix(orbit, cam);
        }

        internal static bool ShouldBlockCombatBoresight(CameraOrbitState orbit)
        {
            if (OrbitInputCache.Frame == Time.frameCount && OrbitInputCache.SustainedManualInput)
                return true;

            return HasManualPanDeviation(orbit);
        }

        internal static bool IsUserManualOrbitView(CameraOrbitState orbit)
            => ShouldBlockCombatBoresight(orbit);

        internal static void PrepareViewSwitch(CameraStateManager cam)
        {
            _suppressUntil = Time.unscaledTime + SmoothCameraConfigCache.ViewSwitchSuppressSeconds;
            States.Clear();
            BoresightLatchHelper.Invalidate(null);
        }

        internal static void PrepareCockpitTransition(CameraStateManager cam)
        {
            float suppress = Mathf.Max(
                SmoothCameraConfigCache.ViewSwitchSuppressSeconds,
                SmoothCameraConfigCache.CockpitEntrySuppressSeconds);
            _suppressUntil = Time.unscaledTime + suppress;
            States.Clear();
            BoresightLatchHelper.Invalidate(null);
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
            BoresightLatchHelper.Invalidate(unit as Aircraft);
        }

        internal static void ResetAll()
        {
            States.Clear();
            BoresightLatchHelper.Invalidate(null);
            _suppressUntil = 0f;
            OrbitInputCache.Frame = -1;
        }

        internal static void ApplyPostMotion(CameraStateManager cam, CameraOrbitState orbitState)
        {
            if (!OrbitRuntimeFlags.OrbitPostMotionActive)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("orbit_post_motion_off");
                return;
            }

            if (Time.unscaledTime < _suppressUntil)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("view_switch_suppress");
                return;
            }

            if (CameraTransitionService.BlockExternalWrites)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("block_external_writes");
                return;
            }

            if (cam.followingRB == null || cam.followingUnit == null || cam.cameraPivot == null)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("missing_follow_target");
                return;
            }

            if (CameraStateManager.cameraMode != CameraMode.orbit)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("not_orbit_mode");
                return;
            }

            if (!GameManager.IsLocalAircraft(cam.followingUnit))
            {
                OrbitCombatRotationHelper.LogBoresightSkip("not_local_aircraft");
                return;
            }

            var aircraft = cam.followingUnit as Aircraft;
            if (aircraft == null)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("not_aircraft");
                return;
            }

            float lookAtLerp = OrbitFieldAccess.GetLookAtTargetLerp(orbitState);
            bool applyCombatBoresight = OrbitCombatRotationHelper.CombatBoresightFrameActive;

            if (!applyCombatBoresight && lookAtLerp > LookAtTargetGate)
            {
                OrbitFramingHelper.ClearTrackingState();
                return;
            }

            Transform body = cam.followingRB.transform;
            OrbitState state = GetState(aircraft);
            float dt = Time.unscaledDeltaTime;
            bool needFraming = OrbitRuntimeFlags.DynamicFramingActive;

            if (applyCombatBoresight)
            {
                bool gunSelected = BoresightAimHelper.IsGunSelected(aircraft);
                UpdateGunTransition(state, gunSelected);

                bool releaseLocked = Time.unscaledTime < state.ReleaseLockUntil;
                bool gunReady = gunSelected
                    && !releaseLocked
                    && state.GunStableFrames >= SmoothCameraConfigCache.GunEngageStableFrames;

                float targetGunWeight = gunReady ? 1f : 0f;
                state.GunWeight = Mathf.MoveTowards(
                    state.GunWeight,
                    targetGunWeight,
                    SmoothCameraConfigCache.GunWeightBlendRate * dt);
            }

            if (needFraming)
            {
                if (!state.FramingInitialized)
                {
                    OrbitFramingHelper.Reset(state.Framing, body);
                    state.FramingInitialized = true;
                }

                OrbitFramingHelper.UpdateSmooth(state.Framing, body, state.GunWeight, dt);
            }

            ApplyOrbitFraming(cam, state, needFraming);

            if (IsUserManualOrbitView(orbitState))
            {
                OrbitCombatRotationHelper.LogBoresightSkip(
                    OrbitInputCache.SustainedManualInput ? "sustained_axis" : "pan_tilt_deviation");

                state.SmoothedWorldRotation = cam.transform.rotation;
                state.RotationInitialized = true;
                InvalidateBoresightLatch(state, aircraft);
                return;
            }

            if (!OrbitRuntimeFlags.CombatFollowActive)
            {
                OrbitCombatRotationHelper.LogBoresightSkip("combat_follow_off");
                return;
            }

            Quaternion targetWorld = BoresightAimHelper.ComputeBoresightWorldRotation(
                aircraft,
                body,
                useLatch: false);

            if (!state.RotationInitialized)
            {
                state.SmoothedWorldRotation = cam.transform.rotation;
                state.RotationInitialized = true;
            }

            float followRate = Mathf.Lerp(
                SmoothCameraConfigCache.CruiseAttitudeFollowRate,
                SmoothCameraConfigCache.GunAttitudeFollowRate,
                state.GunWeight);
            float rotT = 1f - Mathf.Exp(-followRate * dt);
            state.SmoothedWorldRotation = Quaternion.Slerp(state.SmoothedWorldRotation, targetWorld, rotT);
            cam.transform.rotation = state.SmoothedWorldRotation;
            BoresightLatchHelper.UpdateHudLatch(aircraft, state.SmoothedWorldRotation);
        }

        private static void ApplyOrbitFraming(CameraStateManager cam, OrbitState state, bool needFraming)
        {
            if (needFraming)
                OrbitFramingHelper.ApplyFraming(cam, state.Framing);
            else if (OrbitRuntimeFlags.HeightScaleActive)
                OrbitFramingHelper.ApplyBaseHeightScale(cam);
            else
                OrbitFramingHelper.ClearTrackingState();
        }

        internal static void InvalidateBoresightLatch(OrbitState state, Aircraft aircraft)
        {
            state.RotationInitialized = false;
            BoresightLatchHelper.Invalidate(aircraft);
        }

        private static bool HasManualPanDeviation(CameraOrbitState orbit)
        {
            float pan = OrbitInputCache.Frame == Time.frameCount
                ? OrbitInputCache.PanView
                : OrbitFieldAccess.GetPanView(orbit);
            float tilt = OrbitInputCache.Frame == Time.frameCount
                ? OrbitInputCache.TiltView
                : OrbitFieldAccess.GetTiltView(orbit);
            float defaultTilt = SmoothCameraConfigCache.Orbit.DefaultTilt;
            return Mathf.Abs(pan) > ManualPanEpsilon
                || Mathf.Abs(tilt - defaultTilt) > ManualPanEpsilon;
        }

        private static void UpdateGunTransition(OrbitState state, bool gunSelected)
        {
            if (gunSelected && !state.WasGunSelected)
                state.GunStableFrames = 0;
            else if (gunSelected)
                state.GunStableFrames++;
            else if (!gunSelected && state.WasGunSelected)
            {
                state.ReleaseLockUntil = Time.unscaledTime + SmoothCameraConfigCache.CombatReleaseLockoutSeconds;
                state.GunStableFrames = 0;
            }
            else if (!gunSelected)
                state.GunStableFrames = 0;

            state.WasGunSelected = gunSelected;
        }

        private static OrbitState GetState(Unit unit)
        {
            int key = unit.GetInstanceID();
            if (!States.TryGetValue(key, out OrbitState? state))
            {
                state = new OrbitState();
                States[key] = state;
            }
            return state!;
        }
    }
}
