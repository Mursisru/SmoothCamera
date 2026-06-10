namespace NOLoader.SmoothCamera.Services
{
    internal sealed class OrbitState
    {
        internal float GunWeight;
        internal bool WasGunSelected;
        internal float ReleaseLockUntil;
        internal int GunStableFrames;
        internal bool FramingInitialized;

        internal UnityEngine.Quaternion SmoothedWorldRotation = UnityEngine.Quaternion.identity;
        internal bool RotationInitialized;

        internal readonly OrbitFramingState Framing = new OrbitFramingState();
    }
}
