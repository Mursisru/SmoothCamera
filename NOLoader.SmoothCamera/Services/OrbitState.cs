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

        /// <summary>World-Y scalar offset from vanilla orbit (meters).</summary>
        internal float SmoothedVerticalMeters;
        internal bool HeightOffsetInitialized;
        internal Vector3 LastVanillaOrbitPos;

        internal float VisibilityTargetOffset;
        internal float VisibilityAppliedOffset;

        internal readonly OrbitFramingState Framing = new OrbitFramingState();
    }
}
