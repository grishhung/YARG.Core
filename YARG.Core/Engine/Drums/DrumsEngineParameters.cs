using YARG.Core.Engine.Track;

namespace YARG.Core.Engine.Drums
{
    public class DrumsEngineParameters : TrackEngineParameters
    {
        public DrumsEngineParameters()
        {
        }

        public DrumsEngineParameters(double hitWindow, double frontBackRatio, float[] starMultiplierThresholds) : base(hitWindow,
            frontBackRatio, starMultiplierThresholds)
        {
        }
    }
}