using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    internal sealed class OrbitState
    {
        internal float GunWeight;
        internal bool WasGunSelected;
        internal float ReleaseLockUntil;
        internal int GunStableFrames;
        internal int LastWeaponStationNumber = -1;
        internal bool FramingInitialized;

        internal Vector3 SmoothedWorldPosition;
        internal bool PositionInitialized;
        internal Quaternion SmoothedWorldRotation = Quaternion.identity;
        internal bool RotationInitialized;

        internal float VisibilityTargetOffset;
        internal float VisibilityAppliedOffset;

        internal readonly OrbitFramingState Framing = new OrbitFramingState();
    }
}
