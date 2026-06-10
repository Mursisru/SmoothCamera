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
            float dt = OrbitFramingHelper.StableDeltaTime(Time.unscaledDeltaTime);
            bool needFraming = OrbitRuntimeFlags.DynamicFramingActive;

            OrbitFramingHelper.RefreshPitchRate(state.Framing, body, dt);
            OrbitFramingHelper.RefreshOrbitDistance(state.Framing, cam, dt);

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

            Vector3 vanillaOrbitPos = cam.transform.position;

            if (IsUserManualOrbitView(orbitState))
            {
                OrbitCombatRotationHelper.LogBoresightSkip(
                    OrbitInputCache.SustainedManualInput ? "sustained_axis" : "pan_tilt_deviation");

                state.SmoothedModOffset = Vector3.zero;
                state.FilteredTargetModOffset = Vector3.zero;
                state.SmoothedBaseHeightOffset = Vector3.zero;
                state.FilteredTargetInitialized = false;
                state.BaseHeightInitialized = false;
                state.LastVanillaOrbitPos = vanillaOrbitPos;
                state.PositionInitialized = true;
                OrbitVisibilityHelper.Reset(state);
                state.SmoothedWorldRotation = cam.transform.rotation;
                state.RotationInitialized = true;
                InvalidateBoresightLatch(state, aircraft);
                return;
            }

            if (!state.RotationInitialized)
            {
                state.SmoothedWorldRotation = cam.transform.rotation;
                state.RotationInitialized = true;
            }

            if (OrbitRuntimeFlags.CombatFollowActive)
            {
                Quaternion targetWorld = BoresightAimHelper.ComputeBoresightWorldRotation(
                    aircraft,
                    body,
                    useLatch: false);

                float maneuverScale = OrbitFramingHelper.ComputeManeuverSmoothScale(state.Framing);
                float followRate = Mathf.Lerp(
                    SmoothCameraConfigCache.CruiseAttitudeFollowRate,
                    SmoothCameraConfigCache.GunAttitudeFollowRate,
                    state.GunWeight);
                followRate *= maneuverScale;
                followRate = Mathf.Min(
                    followRate,
                    SmoothCameraConfigCache.OrbitCameraRotSmoothRate * Mathf.Max(maneuverScale, ManeuverMinScale));

                float angleErr = Quaternion.Angle(state.SmoothedWorldRotation, targetWorld);
                float microScale = Mathf.Clamp01(angleErr / RotationMicroAngleDegrees);
                followRate *= Mathf.Lerp(0.28f, 1f, microScale);

                float rotT = 1f - Mathf.Exp(-followRate * dt);
                state.SmoothedWorldRotation = Quaternion.Slerp(state.SmoothedWorldRotation, targetWorld, rotT);
                cam.transform.rotation = state.SmoothedWorldRotation;
                BoresightLatchHelper.UpdateHudLatch(aircraft, state.SmoothedWorldRotation);
            }
            else
            {
                OrbitCombatRotationHelper.LogBoresightSkip("combat_follow_off");
            }

            Vector3 viewUp = state.SmoothedWorldRotation * Vector3.up;
            Vector3 baseHeightOffset = SmoothBaseHeightOffset(
                state,
                OrbitFramingHelper.ComputeBaseHeightOffset(cam),
                dt);
            Vector3 dynamicFraming = OrbitFramingHelper.ComputeDynamicFramingOffset(viewUp, state.Framing, needFraming);
            Vector3 virtualCameraPos = vanillaOrbitPos + baseHeightOffset + dynamicFraming + state.SmoothedModOffset;

            Vector3 visOffset = OrbitVisibilityHelper.ComputeVisibilityOffset(
                cam,
                aircraft,
                state,
                lookAtLerp,
                state.Framing.LastPitchRate,
                state.Framing.PitchInitialized ? state.Framing.PrevPitch : 0f,
                dt,
                virtualCameraPos,
                state.SmoothedWorldRotation);

            Vector3 targetModOffset = baseHeightOffset + dynamicFraming + visOffset;
            ApplyModOffsetSmoothing(cam, state, vanillaOrbitPos, targetModOffset, dt);
        }

        private const float ManeuverMinScale = 0.25f;
        private const float PositionDeadbandMeters = 0.014f;
        private const float RotationMicroAngleDegrees = 1.75f;

        private static Vector3 SmoothBaseHeightOffset(OrbitState state, Vector3 target, float dt)
        {
            if (!state.BaseHeightInitialized)
            {
                state.SmoothedBaseHeightOffset = target;
                state.BaseHeightInitialized = true;
                return target;
            }

            float t = 1f - Mathf.Exp(-5.5f * dt);
            state.SmoothedBaseHeightOffset = Vector3.Lerp(state.SmoothedBaseHeightOffset, target, t);
            return state.SmoothedBaseHeightOffset;
        }

        private static void ApplyModOffsetSmoothing(
            CameraStateManager cam,
            OrbitState state,
            Vector3 vanillaOrbitPos,
            Vector3 targetModOffset,
            float dt)
        {
            if (!state.PositionInitialized)
            {
                state.SmoothedModOffset = targetModOffset;
                state.FilteredTargetModOffset = targetModOffset;
                state.FilteredTargetInitialized = true;
                state.LastVanillaOrbitPos = vanillaOrbitPos;
                state.PositionInitialized = true;
            }
            else
            {
                state.LastVanillaOrbitPos = vanillaOrbitPos;
            }

            if (!state.FilteredTargetInitialized)
            {
                state.FilteredTargetModOffset = targetModOffset;
                state.FilteredTargetInitialized = true;
            }
            else
            {
                float preRate = SmoothCameraConfigCache.OrbitCameraPosSmoothRate * 0.5f;
                float preT = 1f - Mathf.Exp(-preRate * dt);
                state.FilteredTargetModOffset = Vector3.Lerp(
                    state.FilteredTargetModOffset,
                    targetModOffset,
                    preT);
            }

            float maneuverScale = OrbitFramingHelper.ComputeManeuverSmoothScale(state.Framing);
            float rate = SmoothCameraConfigCache.OrbitCameraPosSmoothRate;
            rate *= Mathf.Lerp(0.42f, 1f, maneuverScale);

            float speed = cam.followingRB != null ? cam.followingRB.velocity.magnitude : 80f;
            float lowSpeedScale = Mathf.Clamp(speed / 35f, 0.3f, 1f);
            rate *= lowSpeedScale;

            Vector3 delta = state.FilteredTargetModOffset - state.SmoothedModOffset;
            float deadbandSq = PositionDeadbandMeters * PositionDeadbandMeters;
            if (delta.sqrMagnitude > deadbandSq)
            {
                float t = 1f - Mathf.Exp(-rate * dt);
                state.SmoothedModOffset += delta * t;
            }

            cam.transform.position = vanillaOrbitPos + state.SmoothedModOffset;
        }

        internal static void InvalidateBoresightLatch(OrbitState state, Aircraft aircraft)
        {
            state.RotationInitialized = false;
            state.PositionInitialized = false;
            state.FilteredTargetInitialized = false;
            state.BaseHeightInitialized = false;
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
