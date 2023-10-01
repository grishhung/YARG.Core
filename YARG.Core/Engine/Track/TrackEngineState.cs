namespace YARG.Core.Engine.Track
{
    public abstract class TrackEngineState : BaseEngineState
    {
        public int  CurrentSoloIndex;
        public bool IsSoloActive;

        public override void Reset()
        {
            base.Reset();

            CurrentSoloIndex = 0;
            IsSoloActive = false;
        }
    }
}