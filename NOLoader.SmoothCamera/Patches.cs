using NOLoader.SmoothCamera.Services;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NOLoader.SmoothCamera
{
    internal static class Patches
    {
        private static CameraStateManager? _cachedCamMgr;
        private static int _cachedCamMgrFrame = -1;
        private static float _lastGlocBp = -1f;
        private static int _lastGlocFrame = -1;

        public static void CameraOrbitMotionPrefix(CameraOrbitState __instance, CameraStateManager cam)
        {
            if (CameraTransitionService.BlockExternalWrites)
                return;
            if (cam.currentState != __instance)
                return;

            float axisX = 0f;
            float axisY = 0f;
            if (GameManager.playerInput != null && !Cursor.visible)
            {
                axisX = GameManager.playerInput.GetAxis("Pan View");
                axisY = GameManager.playerInput.GetAxis("Tilt View");
            }

            float panView = OrbitFieldAccess.GetPanView(__instance);
            float tiltView = OrbitFieldAccess.GetTiltView(__instance);
            OrbitCameraController.ProcessOrbitMotionPrefix(
                __instance,
                cam,
                ref panView,
                ref tiltView,
                axisX,
                axisY);
            OrbitFieldAccess.SetPanView(__instance, panView);
            OrbitFieldAccess.SetTiltView(__instance, tiltView);
        }

        public static void CameraOrbitMotionPostfix(CameraOrbitState __instance, CameraStateManager cam)
        {
            if (CameraTransitionService.BlockExternalWrites)
                return;
            if (cam.currentState != __instance)
                return;

            OrbitCameraController.ApplyPostMotion(cam, __instance);
        }

        public static void CameraOrbitInputsPostfix(CameraOrbitState __instance, CameraStateManager cam)
        {
            if (CameraTransitionService.BlockExternalWrites)
                return;
            if (cam.currentState != __instance)
                return;
            if (OrbitInputCache.Frame == Time.frameCount)
                return;

            float axisX = 0f;
            float axisY = 0f;
            if (GameManager.playerInput != null && !Cursor.visible)
            {
                axisX = GameManager.playerInput.GetAxis("Pan View");
                axisY = GameManager.playerInput.GetAxis("Tilt View");
            }

            float panView = OrbitFieldAccess.GetPanView(__instance);
            float tiltView = OrbitFieldAccess.GetTiltView(__instance);
            OrbitCameraController.RecordOrbitInput(panView, tiltView, axisX, axisY);
        }

        public static void CameraOrbitEnterPostfix(CameraStateManager cam)
        {
            OrbitCameraController.NeutralizeExternalTransform(cam);
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }

        public static void CameraOrbitLeavePostfix(CameraOrbitState __instance, CameraStateManager cam)
        {
            OrbitCameraController.NeutralizeExternalTransform(cam);
            AutoCenterController.Clear(__instance);
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }

        public static void CameraChaseCheckHudPrefix(CameraChaseState __instance)
        {
            if (!SmoothCameraConfigCache.Enabled || !SmoothCameraConfigCache.ExternalHudEnabled)
                return;
            if (!SmoothCameraConfigCache.Chase.ForceExternalHud)
                return;

            var cam = GetCachedCameraManager();
            if (cam == null || cam.followingUnit == null || !GameManager.IsLocalAircraft(cam.followingUnit))
                return;

            OrbitFieldAccess.SetChaseShowHud(__instance, true);
        }

        public static void CameraTvUpdatePostfix(CameraTVState __instance, CameraStateManager cam)
        {
            if (!CameraTransitionService.ShouldApplyExternalWrite(cam, __instance))
                return;
            if (!SmoothCameraConfigCache.IsModeEnabled(CameraMode.tv))
                return;

            float axisX = 0f;
            float axisY = 0f;
            if (GameManager.playerInput != null && !Cursor.visible)
            {
                axisX = GameManager.playerInput.GetAxis("Pan View");
                axisY = GameManager.playerInput.GetAxis("Tilt View");
            }

            Vector2 panTiltView = OrbitFieldAccess.GetTvPanTiltView(__instance);
            Vector2 desiredPanTiltView = OrbitFieldAccess.GetTvDesiredPanTiltView(__instance);
            AutoCenterController.ProcessTvPanTilt(
                __instance,
                SmoothCameraConfigCache.Tv,
                ref panTiltView,
                ref desiredPanTiltView,
                axisX,
                axisY);
            OrbitFieldAccess.SetTvPanTiltView(__instance, panTiltView);
            OrbitFieldAccess.SetTvDesiredPanTiltView(__instance, desiredPanTiltView);
        }

        public static void CameraTvLeavePostfix(CameraTVState __instance, CameraStateManager cam)
        {
            AutoCenterController.Clear(__instance);
        }

        public static void CameraCockpitEnterPrefix(CameraCockpitState __instance)
        {
            PatchReflection.ResetCockpitEnterState(__instance);
        }

        public static void CameraCockpitLeavePostfix(CameraStateManager cam)
        {
            if (cam.followingUnit != null)
                OrbitCameraController.Reset(cam.followingUnit);
        }

        public static void CameraStateManagerSwitchPrefix(CameraStateManager __instance, CameraBaseState state)
        {
            CameraTransitionService.OnSwitchStatePrefix(__instance, state);
            if (state != __instance.cockpitState)
                OrbitCameraController.PrepareViewSwitch(__instance);
        }

        public static void CameraStateManagerSwitchPostfix(CameraStateManager __instance)
        {
            CameraTransitionService.OnSwitchStatePostfix(__instance);
            _cachedCamMgr = null;
            _cachedCamMgrFrame = -1;
        }

        public static void CameraStateManagerFollowingPostfix(CameraStateManager __instance)
        {
            ExternalHudOrchestrator.Refresh(__instance);
            _cachedCamMgr = __instance;
            _cachedCamMgrFrame = Time.frameCount;
        }

        public static void DynamicMapMinimizePostfix()
        {
            if (ExternalHudOrchestrator.ShouldShowMinimap())
                DynamicMap.EnableCanvas(true);
        }

        public static void GlocSimulateExternalPostfix(GLOC __instance)
        {
            if (!SmoothCameraConfigCache.GlocEffectsInExternalViews)
                return;
            if (CameraStateManager.cameraMode == CameraMode.cockpit)
                return;

            var cam = GetCachedCameraManager();
            if (cam == null || cam.followingUnit == null || !GameManager.IsLocalAircraft(cam.followingUnit))
                return;
            if (!ExternalHudOrchestrator.IsExternalFlightMode(CameraStateManager.cameraMode))
                return;

            float bp = PatchReflection.GetGlocBloodPressure(__instance);
            int frame = Time.frameCount;
            if (_lastGlocFrame >= 0
                && frame - _lastGlocFrame < 2
                && Mathf.Abs(bp - _lastGlocBp) < 0.02f)
                return;

            _lastGlocFrame = frame;
            _lastGlocBp = bp;

            var blackout = PatchReflection.GetGlocBlackoutImage(__instance);
            var colorAdj = PatchReflection.GetGlocColorAdjustments(__instance);
            var vignette = PatchReflection.GetGlocVignette(__instance);
            if (blackout == null || colorAdj == null || vignette == null)
                return;

            float fade = (bp - 0.2f) / 0.4f;
            float sat = (bp - 0.3f) / 0.4f;
            blackout.color = Color.Lerp(Color.black, Color.clear, fade);
            colorAdj.saturation.value = Mathf.Lerp(-100f, 0f, sat);
            vignette.intensity.value = Mathf.Lerp(1f, 0.4f, fade);
            AudioMixerVolume.SetMasterAudioFilterStrength(
                Mathf.Lerp(250f, 11000f, Mathf.Clamp01(fade)) + 11000f * Mathf.Clamp01(sat));
        }

        public static bool GlocSwitchCameraPrefixSkip()
        {
            if (!SmoothCameraConfigCache.GlocEffectsInExternalViews)
                return true;
            if (CameraStateManager.cameraMode == CameraMode.cockpit)
                return true;

            var cam = GetCachedCameraManager();
            if (cam == null || cam.followingUnit == null)
                return true;
            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return true;
            if (!ExternalHudOrchestrator.IsExternalFlightMode(CameraStateManager.cameraMode))
                return true;

            return false;
        }

        public static void HudBoresightReticlePostfix(HUDBoresightState __instance, Aircraft aircraft)
        {
            var camMgr = GetCachedCameraManager();
            if (camMgr == null || !HudFramingCompensationHelper.ShouldCompensate(camMgr))
                return;

            Image? boresight = OrbitFieldAccess.GetBoresightImage(__instance);
            if (boresight == null || aircraft == null || camMgr?.mainCamera == null)
                return;

            Vector3 vanillaReticle = boresight.transform.position;
            Vector3 reticle = HudFramingCompensationHelper.ComputeSmoothedReticleScreen(
                aircraft,
                camMgr.mainCamera,
                vanillaReticle);
            reticle.z = 0f;
            boresight.transform.position = reticle;
        }


        private static CameraStateManager? GetCachedCameraManager()
        {
            int frame = Time.frameCount;
            if (_cachedCamMgr != null && frame - _cachedCamMgrFrame <= 1)
                return _cachedCamMgr;

            _cachedCamMgr = SceneSingleton<CameraStateManager>.i;
            _cachedCamMgrFrame = frame;
            return _cachedCamMgr;
        }
    }
}
