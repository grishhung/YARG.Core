using YARG.Core.Chart;
using YARG.Core.Engine.Track;

namespace YARG.Core.Engine.Drums
{
    public class DrumsEngineState : TrackEngineState
    {
        /// <summary>
        /// The integer value for the pad that was inputted this update. <c>-1</c> is none, and the value can
        /// be based off of <see cref="FourLaneDrumPad"/> or <see cref="FiveLaneDrumPad"/>.
        /// </summary>
        public int PadHitThisUpdate = -1;

        public override void Reset()
        {
            base.Reset();

            PadHitThisUpdate = -1;
        }
    }
}