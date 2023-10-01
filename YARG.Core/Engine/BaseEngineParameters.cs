using System;
using System.IO;
using YARG.Core.Utility;

namespace YARG.Core.Engine
{
    public abstract class BaseEngineParameters : IBinarySerializable
    {
        public float[] StarMultiplierThresholds { get; private set; }

        protected BaseEngineParameters()
        {
            StarMultiplierThresholds = Array.Empty<float>();
        }

        protected BaseEngineParameters(float[] starMultiplierThresholds)
        {
            StarMultiplierThresholds = starMultiplierThresholds;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
        }

        public virtual void Deserialize(BinaryReader reader, int version = 0)
        {
        }
    }
}