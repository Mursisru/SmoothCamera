using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal static class HudFramingCompensationHelper
    {
        private const float GunAimDistance = 3000f;
        internal const float OffsetThresholdSq = 4f;
        private const float ReticleSmoothRate = 14f;
        private const int TargetRecomputeInterval = 4;

        private static Vector3 _smoothedReticle;
        private static Vector3 _targetReticle;
        private static float _cachedOffsetY;
        private static int _lastTargetFrame = -1;
        private static bool _reticleInitialized;

        internal static bool HasActiveFramingOffset()
            => OrbitFramingHelper.LastDynamicFramingOffset.sqrMagnitude > OffsetThresholdSq;

        internal static bool ShouldCompensate(CameraStateManager cam)
        {
            if (!OrbitRuntimeFlags.HudCompensationActive)
                return false;
            if (cam == null || cam.followingUnit == null || cam.mainCamera == null)
                return false;
            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return false;
            if (CameraStateManager.cameraMode != CameraMode.orbit)
                return false;
            if (cam.currentState != cam.orbitState)
                return false;

            return HasActiveFramingOffset();
        }

        internal static Vector3 ComputeSmoothedReticleScreen(Aircraft aircraft, Camera camera, Vector3 vanillaReticle)
        {
            float dt = Time.unscaledDeltaTime;
            float offsetY = OrbitFramingHelper.LastDynamicFramingOffset.y;
            int frame = Time.frameCount;

            bool recomputeTarget = !_reticleInitialized
                || frame - _lastTargetFrame >= TargetRecomputeInterval
                || Mathf.Abs(offsetY - _cachedOffsetY) > 0.08f;

            if (recomputeTarget)
            {
                _targetReticle = ComputeTargetReticle(aircraft, camera, vanillaReticle, offsetY);
                _lastTargetFrame = frame;
                _cachedOffsetY = offsetY;
                if (!_reticleInitialized)
                {
                    _smoothedReticle = _targetReticle;
                    _reticleInitialized = true;
                }
            }

            float smoothT = 1f - Mathf.Exp(-ReticleSmoothRate * dt);
            _smoothedReticle.x += (_targetReticle.x - _smoothedReticle.x) * smoothT;
            _smoothedReticle.y += (_targetReticle.y - _smoothedReticle.y) * smoothT;
            _smoothedReticle.z = 0f;
            return _smoothedReticle;
        }

        internal static void ResetReticleSmoothing()
        {
            _reticleInitialized = false;
            _lastTargetFrame = -1;
        }

        private static Vector3 ComputeTargetReticle(Aircraft aircraft, Camera camera, Vector3 vanillaReticle, float offsetY)
        {
            if (offsetY * offsetY <= OffsetThresholdSq)
                return vanillaReticle;

            Vector3 aimWorldPoint = ComputeGunAimWorld(aircraft);
            Vector3 actualScreen = camera.WorldToScreenPoint(aimWorldPoint);
            actualScreen.z = 0f;

            Vector3 shift = ComputeFramingScreenShift(
                camera,
                actualScreen,
                aimWorldPoint,
                OrbitFramingHelper.LastDynamicFramingOffset);
            Vector3 reticle = actualScreen - shift;
            reticle.z = 0f;
            return reticle;
        }

        internal static Vector3 ComputeGunAimWorld(Aircraft aircraft)
        {
            Vector3 gunDir = BoresightAimHelper.GetGunDirectionWorld(aircraft);
            return aircraft.transform.position + gunDir * GunAimDistance;
        }

        internal static Vector3 ComputeFramingScreenShift(
            Camera camera, Vector3 actualScreen, Vector3 worldPoint, Vector3 framingWorldOffset)
        {
            if (framingWorldOffset.sqrMagnitude < 1e-8f)
                return Vector3.zero;

            Vector3 virtualCamPos = camera.transform.position - framingWorldOffset;
            Vector3 reference = WorldToScreenAtCamPos(camera, virtualCamPos, camera.transform.rotation, worldPoint);
            reference.z = 0f;
            return actualScreen - reference;
        }

        private static Vector3 WorldToScreenAtCamPos(Camera camera, Vector3 camPos, Quaternion camRot, Vector3 worldPoint)
        {
            Vector3 local = Quaternion.Inverse(camRot) * (worldPoint - camPos);
            if (local.z <= 0.01f)
                return new Vector3(-9999f, -9999f, 0f);

            float tanHalfFov = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float invZ = 1f / (local.z * tanHalfFov);
            float x = local.x * invZ / camera.aspect;
            float y = local.y * invZ;
            float px = (x + 1f) * 0.5f * camera.pixelWidth;
            float py = (y + 1f) * 0.5f * camera.pixelHeight;
            return new Vector3(px, py, 0f);
        }
    }
}
