namespace SSEGL1Viewer.Protocol;

/// <summary>
/// CRC-16/CCITT-FALSE
///
/// Width  : 16
/// Poly   : 0x1021
/// Init   : 0xFFFF
/// RefIn  : false
/// RefOut : false
/// XorOut : 0x0000
/// </summary>
internal static class Crc16Ccitt
{
    private const ushort Polynomial = 0x1021;
    private const ushort InitialValue = 0xFFFF;

    public static ushort Calculate(ReadOnlySpan<byte> data)
    {
        ushort crc = InitialValue;

        foreach (byte value in data)
        {
            crc ^= (ushort)(value << 8);

            for (int bit = 0; bit < 8; bit++)
            {
                bool highBitSet = (crc & 0x8000) != 0;

                crc <<= 1;

                if (highBitSet)
                {
                    crc ^= Polynomial;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// CRCを上位バイト、下位バイトの順で返します。
    /// 実際のSSC送信順はTransport::sendの確認後に確定します。
    /// </summary>
    public static byte[] GetBigEndianBytes(ReadOnlySpan<byte> data)
    {
        ushort crc = Calculate(data);

        return
        [
            (byte)(crc >> 8),
            (byte)(crc & 0xFF)
        ];
    }

    /// <summary>
    /// CRCを下位バイト、上位バイトの順で返します。
    /// </summary>
    public static byte[] GetLittleEndianBytes(ReadOnlySpan<byte> data)
    {
        ushort crc = Calculate(data);

        return
        [
            (byte)(crc & 0xFF),
            (byte)(crc >> 8)
        ];
    }
}