namespace SSEGL1Viewer.Transport
{
    public sealed class SSCReply
    {
        public byte Tid { get; init; }

        public byte Command { get; init; }

        public byte Category { get; init; }

        public byte Flags { get; init; }

        public uint SourceAddress { get; init; }

        public uint DestinationAddress { get; init; }

        public byte[] Payload { get; init; } = Array.Empty<byte>();
    }
}