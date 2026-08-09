namespace SSEGL1Viewer.Transport
{
    public sealed class GL1DecodedSample
    {
        public uint Index { get; init; }

        /// <summary>
        /// Data ID 0/1では前半3成分、
        /// Data ID 2/3では唯一の3成分です。
        /// </summary>
        public MotionVector3 Primary { get; init; }

        /// <summary>
        /// Data ID 0/1に存在する後半3成分です。
        /// Data ID 2/3ではnullです。
        /// </summary>
        public MotionVector3? Secondary { get; init; }
    }
}