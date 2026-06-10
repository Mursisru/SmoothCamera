using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal sealed class OrbitState
    {
        internal float GunWeight;
        internal bool WasGunSelected;
        internal float ReleaseLockUntil;
        internal int GunStableFrames;
        internal bool FramingInitialized;

        internal Quaternion SmoothedWorldRotation = Quaternion.identity;
        internal bool RotationInitialized;

        /// <summary>Smoothed mod offset (base height + dynamic framing + visibility).</summary>
        internal Vector3 SmoothedModOffset;
        internal Vector3 FilteredTargetModOffset;
        internal Vector3 SmoothedBaseHeightOffset;
        internal bool FilteredTargetInitialized;
        internal bool BaseHeightInitialized;
        internal Vector3 LastVanillaOrbitPos;
        internal bool PositionInitialized;

        internal float VisibilityTargetOffset;
        internal float VisibilityAppliedOffset;

        internal readonly OrbitFramingState Framing = new OrbitFramingState();
    }
}
