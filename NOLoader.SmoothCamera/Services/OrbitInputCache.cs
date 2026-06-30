namespace NOLoader.SmoothCamera.Services
{
    /// <summary>One input sample per frame shared between orbit prefix and inputs postfix.</summary>
    internal static class OrbitInputCache
    {
        private const float RawManualThresholdSqr = 0.0025f;
        private const float SustainedAxisThresholdSqr = 0.0225f;
        private const int SustainedFramesRequired = 2;

        private static int _sustainedFrames;

        internal static int Frame = -1;
        internal static float PanView;
        internal static float TiltView;
        internal static float AxisX;
        internal static float AxisY;
        internal static bool ManualInput;
        internal static bool SustainedManualInput;

        internal static void Capture(int frame, float pan, float tilt, float axisX, float axisY)
        {
            Frame = frame;
            PanView = pan;
            TiltView = tilt;
            AxisX = axisX;
            AxisY = axisY;

            float axisMagSqr = axisX * axisX + axisY * axisY;
            ManualInput = axisMagSqr > RawManualThresholdSqr;

            if (axisMagSqr > SustainedAxisThresholdSqr)
                _sustainedFrames++;
            else
                _sustainedFrames = 0;

            SustainedManualInput = _sustainedFrames >= SustainedFramesRequired;
        }
    }
}
