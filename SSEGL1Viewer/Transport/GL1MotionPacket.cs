using System;
using System.Collections.Generic;

namespace SSEGL1Viewer.Transport
{
    public sealed class GL1MotionPacket
    {
        public DateTime BaseTimestamp { get; init; }

        public ushort Milliseconds { get; init; }

        public uint StartIndex { get; init; }

        public byte SampleCount { get; init; }

        public byte ChannelFlags { get; init; }

        public ushort SamplingFrequency { get; init; }

        public byte QuantizationMode { get; init; }

        public IReadOnlyList<MotionSample> Samples { get; init; } =
            Array.Empty<MotionSample>();
    }
}