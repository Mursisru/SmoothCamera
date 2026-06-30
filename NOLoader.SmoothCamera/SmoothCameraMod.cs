using NOLoader.API;
using NOLoader.ModConfig;
using NOLoader.SmoothCamera.Services;
using UnityEngine;

namespace NOLoader.SmoothCamera
{
    public sealed class SmoothCameraMod : INOMod
    {
        public void OnLoad(ref NOModContext ctx)
        {
            SmoothCameraConfigCache.Load(ModIniConfig.Load(ctx.ModRoot));
#if NOLoader_DEV
            Debug.Log("[NOLoader] SmoothCamera loaded");
#endif
        }

        public void OnUnload(ref NOModContext ctx)
        {
            OrbitCameraController.ResetAll();
            AutoCenterController.ClearAll();
            CameraTransitionService.ResetAll();
            OrbitFramingHelper.ClearTrackingState();
        }
    }
}
