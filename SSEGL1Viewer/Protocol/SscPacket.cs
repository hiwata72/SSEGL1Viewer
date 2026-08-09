namespace SSEGL1Viewer.Protocol;

/// <summary>
/// libssc.so内でTransport::sendへ渡されるSSC Packet。
/// </summary>
internal sealed class SscPacket
{
    public SscPacket(
        byte transactionId,
        byte requestDefinition,
        uint sourceAddress,
        uint destinationAddress,
        uint mode,
        ReadOnlySpan<byte> payload)
    {
        if (transactionId > 15)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transactionId),
                "Transaction IDは0～15で指定してください。");
        }

        if (payload.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payload),
                "Payloadが65535バイトを超えています。");
        }

        TransactionId = transactionId;
        RequestDefinition = requestDefinition;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        Mode = mode;
        Payload = payload.ToArray();
    }

    /// <summary>
    /// Transport内部では上位4bitへ配置されます。
    /// </summary>
    public byte TransactionId { get; }

    public byte RequestDefinition { get; }

    public ushort PayloadLength => checked((ushort)Payload.Length);

    public uint SourceAddress { get; }

    public uint DestinationAddress { get; }

    public uint Mode { get; }

    public byte[] Payload { get; }

    /// <summary>
    /// Packet先頭バイトに相当する値。
    /// </summary>
    public byte TransactionByte => (byte)(TransactionId << 4);
}