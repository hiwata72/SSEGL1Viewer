namespace SSEGL1Viewer.Protocol;

internal static class Gl1Commands
{
    public static ReadOnlySpan<byte> StartPayload =>
    [
        0x02,
        0x01,
        0x0C
    ];

    public const byte RequestDestination = 0x80;
    public const byte RequestCommand = 0x01;

    public const uint DefaultMode = 3;

    public static SscPacket CreateStartPacket(
        byte transactionId = 0,
        uint mode = DefaultMode)
    {
        return new SscPacket(
            transactionId: transactionId,
            requestDefinition: RequestCommand,
            sourceAddress: 0x08000000,
            destinationAddress:
                (uint)RequestDestination << 24,
            mode: mode,
            payload: StartPayload);
    }
}