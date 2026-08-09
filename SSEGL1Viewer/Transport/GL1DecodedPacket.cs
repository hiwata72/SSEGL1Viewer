using System;
using System.Collections.Generic;

namespace SSEGL1Viewer.Transport
{
    public sealed class GL1DecodedPacket
    {
        public int DataId { get; init; }

        public bool IsDeltaTime { get; init; }

        public bool IsCompressed { get; init; }

        /// <summary>
        /// 2001-01-01 UTCを基準とした機器時刻を
        /// DateTimeOffsetへ変換したものです。
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        public ulong TimestampNanoseconds { get; init; }

        public IReadOnlyList<GL1DecodedSample> Samples { get; init; } =
            Array.Empty<GL1DecodedSample>();
    }
}