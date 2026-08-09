namespace SSEGL1Viewer.Protocol;

internal enum CrcByteOrder
{
    BigEndian,
    LittleEndian
}

internal static class SscFrameBuilder
{
    public static byte[] Build(
        byte[] header,
        SscPacket packet,
        CrcByteOrder order)
    {
        ushort crc = Crc16Ccitt.Calculate(packet.Payload);

        byte[] frame = new byte[
            header.Length +
            packet.Payload.Length +
            2];

        int p = 0;

        Buffer.BlockCopy(header, 0, frame, p, header.Length);
        p += header.Length;

        Buffer.BlockCopy(packet.Payload, 0, frame, p, packet.Payload.Length);
        p += packet.Payload.Length;

        if (order == CrcByteOrder.BigEndian)
        {
            frame[p++] = (byte)(crc >> 8);
            frame[p] = (byte)crc;
        }
        else
        {
            frame[p++] = (byte)crc;
            frame[p] = (byte)(crc >> 8);
        }

        return frame;
    }
}