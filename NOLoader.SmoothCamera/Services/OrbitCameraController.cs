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

        internal static bool ShouldBlockCombatBoresight(CameraOrbitState orbit, Aircraft? aircraft = null)
        {
            if (AutoCenterController.IsRecenteringOrGrace(orbit))
                return false;

            if (OrbitRuntimeFlags.CombatFollowActive)
                return IsSustainedManualOverride();

            if (IsSustainedManualOverride())
                return true;

            return HasManualPanDeviation(orbit);
        }

        internal static bool IsUserManualOrbitView(CameraOrbitState orbit, Aircraft? aircraft = null)
            => ShouldBlockCombatBoresight(orbit, aircraft);

        private static bool IsSustainedManualOverride()
            => OrbitInputCache.Frame == Time.frameCount && OrbitInputCache.SustainedManualInput;

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
                OrbitHeightController.Reset(GetState(aircraft));
                return;
            }

            Transform body = cam.followingRB.transform;
            OrbitState state = GetState(aircraft);
            float dt = OrbitFramingHelper.StableDeltaTime(Time.unscaledDeltaTime);
            bool needFraming = OrbitRuntimeFlags.DynamicFramingActive;
            Vector3 vanillaOrbitPos = cam.transform.position;
            Quaternion vanillaRotation = cam.transform.rotation;

            float angularRateDeg = OrbitDynamicsHelper.GetAngularRateDeg(
                cam.followingRB,
                state.Framing.LastPitchRate);

            OrbitFramingHelper.RefreshPitchRate(state.Framing, body, angularRateDeg, dt);
            angularRateDeg = OrbitDynamicsHelper.GetAngularRateDeg(
                cam.followingRB,
                state.Framing.LastPitchRate);
            OrbitFramingHelper.RefreshOrbitDistance(state.Framing, cam, vanillaOrbitPos, dt);

            if (needFraming && !state.FramingInitialized)
            {
                OrbitFramingHelper.Reset(state.Framing, body);
                state.FramingInitialized = true;
            }

            bool weaponActive = BoresightAimHelper.HasActiveWeaponStation(aircraft);
            bool combatAimActive = OrbitRuntimeFlags.CombatFollowActive && weaponActive;

            if (combatAimActive)
                UpdateCombatEngage(state, !IsSustainedManualOverride(), aircraft, dt);

            if (IsSustainedManualOverride())
            {
                OrbitCombatRotationHelper.LogBoresightSkip("sustained_axis");

                OrbitHeightController.Reset(state);
                OrbitVisibilityHelper.Reset(state);
                OrbitPresentComposer.SyncFromCamera(state, cam.transform.rotation);
                InvalidateBoresightLatch(state, aircraft);
                return;
            }

            float framingWeight = combatAimActive ? state.GunWeight : 0f;
            Quaternion targetRotation = combatAimActive
                ? OrbitRotationComposer.ComputeTargetRotation(aircraft, body, vanillaRotation)
                : vanillaRotation;

            OrbitPresentComposer.PresentRotation(
                cam,
                state,
                aircraft,
                targetRotation,
                angularRateDeg,
                dt);

            float targetVerticalMeters = OrbitHeightController.ComputeTargetVerticalMeters(
                cam,
                state.Framing,
                body,
                needFraming,
                framingWeight,
                angularRateDeg,
                dt);

            OrbitHeightController.Apply(
                cam,
                state,
                vanillaOrbitPos,
                targetVerticalMeters,
                angularRateDeg,
                dt);
        }

        internal static void InvalidateBoresightLatch(OrbitState state, Aircraft aircraft)
        {
            state.RotationInitialized = false;
            OrbitHeightController.Reset(state);
            OrbitVisibilityHelper.Reset(state);
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

        /// <summary>Exact gun/VPU engage ramp — keyed on any active weapon station, not gun flag.</summary>
        private static void UpdateCombatEngage(OrbitState state, bool weaponEngaged, Aircraft aircraft, float dt)
        {
            int stationNumber = aircraft.weaponManager?.currentWeaponStation?.Number ?? -1;
            if (stationNumber != state.LastWeaponStationNumber)
            {
                state.LastWeaponStationNumber = stationNumber;
                state.GunStableFrames = 0;
                state.WasGunSelected = false;
            }

            if (weaponEngaged && !state.WasGunSelected)
                state.GunStableFrames = 0;
            else if (weaponEngaged)
                state.GunStableFrames++;
            else if (!weaponEngaged && state.WasGunSelected)
            {
                state.ReleaseLockUntil = Time.unscaledTime + SmoothCameraConfigCache.CombatReleaseLockoutSeconds;
                state.GunStableFrames = 0;
            }
            else if (!weaponEngaged)
                state.GunStableFrames = 0;

            state.WasGunSelected = weaponEngaged;

            bool releaseLocked = Time.unscaledTime < state.ReleaseLockUntil;
            bool combatReady = weaponEngaged
                && !releaseLocked
                && state.GunStableFrames >= SmoothCameraConfigCache.GunEngageStableFrames;

            if (combatReady)
                state.GunWeight = 1f;
            else
            {
                state.GunWeight = Mathf.MoveTowards(
                    state.GunWeight,
                    0f,
                    SmoothCameraConfigCache.GunWeightBlendRate * dt);
            }
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
