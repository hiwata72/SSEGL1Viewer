namespace SSEGL1Viewer.Transport
{
    public sealed class MotionSample
    {
        /// <summary>
        /// Payload内でのサンプル番号です。
        /// </summary>
        public uint Index { get; init; }

        /// <summary>
        /// 推定サンプル時刻です。
        /// </summary>
        public DateTime Timestamp { get; init; }

        public MotionVector3? Acceleration { get; init; }

        public MotionVector3? Gyroscope { get; init; }

        /// <summary>
        /// ChannelFlags bit2のデータです。
        /// 現段階では意味未確定のため、読み飛ばします。
        /// </summary>
        public bool HasUnknownVectorChannel { get; init; }

        /// <summary>
        /// ChannelFlags bit3のデータです。
        /// 現段階では意味未確定のため、読み飛ばします。
        /// </summary>
        public bool HasFeatureChannel { get; init; }
    }
}
