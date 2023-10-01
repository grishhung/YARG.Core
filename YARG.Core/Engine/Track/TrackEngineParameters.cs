using System;
using System.IO;

namespace YARG.Core.Engine.Track
{
    public abstract class TrackEngineParameters : BaseEngineParameters
    {
        // Don't use this value in logic, use the FrontEnd and BackEnd (-1/2 and +1/2 values respectively)
        public double HitWindow        { get; }
        public double FrontToBackRatio { get; }

        /// <summary>
        /// How much time ahead of the strikeline can a note be hit. This value is always negative.
        /// </summary>
        public double FrontEnd { get; private set; }

        /// <summary>
        /// How much time behind the strikeline can a note be hit. This value is always positive.
        /// </summary>
        public double BackEnd { get; private set; }

        protected TrackEngineParameters()
        {
        }

        protected TrackEngineParameters(double hitWindow, double frontBackRatio, float[] starMultiplierThresholds)
            : base(starMultiplierThresholds)
        {
            HitWindow = hitWindow;
            FrontToBackRatio = frontBackRatio;

            FrontEnd = -(Math.Abs(HitWindow / 2) * FrontToBackRatio);
            BackEnd = Math.Abs(HitWindow / 2) * (2 - FrontToBackRatio);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(FrontEnd);
            writer.Write(BackEnd);
        }

        public override void Deserialize(BinaryReader reader, int version = 0)
        {
            base.Deserialize(reader, version);

            FrontEnd = reader.ReadDouble();
            BackEnd = reader.ReadDouble();
        }
    }
}