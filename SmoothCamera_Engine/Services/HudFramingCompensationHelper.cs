using SmoothCamera_Engine.Config;
using UnityEngine;

namespace SmoothCamera_Engine.Services
{
    internal static class HudFramingCompensationHelper
    {
        private const float GunAimDistance = 3000f;
        private const float DriveThreshold = 0.04f;
        private const float OffsetThresholdSq = 1e-4f;

        internal static bool ShouldCompensate(CameraStateManager cam)
        {
            if (!SmoothCameraConfig.Enabled.Value || !SmoothCameraConfig.AlignHudToBoresight.Value)
                return false;
            if (cam == null || cam.followingUnit == null || cam.mainCamera == null)
                return false;
            if (!GameManager.IsLocalAircraft(cam.followingUnit))
                return false;
            if (CameraStateManager.cameraMode != CameraMode.orbit)
                return false;
            if (cam.currentState != cam.orbitState)
                return false;
            if (SmoothCameraConfig.OrbitDynamicFramingStrength.Value <= 0.001f)
                return false;

            return OrbitFramingHelper.LastDynamicFramingOffset.sqrMagnitude > OffsetThresholdSq
                || Mathf.Abs(OrbitFramingHelper.LastSmoothedDrive) > DriveThreshold;
        }

        internal static Vector3 ComputeGunAimWorld(Aircraft aircraft)
        {
            Vector3 gunDir = BoresightAimHelper.GetGunDirectionWorld(aircraft);
            return aircraft.transform.position + gunDir * GunAimDistance;
        }

        internal static Vector3 ComputeCompensatedReticleScreen(Aircraft aircraft, Camera camera)
        {
            Vector3 aimWorld = ComputeGunAimWorld(aircraft);
            Vector3 screen = camera.WorldToScreenPoint(aimWorld);
            screen.z = 0f;

            Vector3 dynamicOffset = OrbitFramingHelper.LastDynamicFramingOffset;
            if (dynamicOffset.sqrMagnitude <= OffsetThresholdSq)
                return screen;

            Vector3 shift = ComputeFramingScreenShift(camera, screen, aimWorld, dynamicOffset);
            return screen - shift;
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
            float x = local.x / (local.z * tanHalfFov * camera.aspect);
            float y = local.y / (local.z * tanHalfFov);
            float px = (x + 1f) * 0.5f * camera.pixelWidth;
            float py = (y + 1f) * 0.5f * camera.pixelHeight;
            return new Vector3(px, py, 0f);
        }
    }
}
