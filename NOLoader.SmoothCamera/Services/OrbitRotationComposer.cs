using UnityEngine;

namespace NOLoader.SmoothCamera.Services
{
    /// <summary>Builds target boresight rotation; presentation smoothing is in OrbitPresentComposer.</summary>
    internal static class OrbitRotationComposer
    {
        internal static Quaternion ComputeTargetRotation(
            Aircraft aircraft,
            Transform body,
            Quaternion fallbackRotation)
        {
            if (OrbitRuntimeFlags.DiagnosticInstantBoresight)
                return BoresightAimHelper.ComputeBoresightWorldRotation(aircraft, body);

            return BoresightAimHelper.ComputeBoresightWorldRotation(aircraft, body);
        }
    }
}
