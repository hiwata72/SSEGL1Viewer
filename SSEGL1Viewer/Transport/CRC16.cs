namespace SSEGL1Viewer.Transport
{
    /// <summary>
    /// CRC-16/CCITT (Poly=0x1021, Initial=0xFFFF)
    /// ※CRCを計算するだけであり、計算対象データの選択は呼び出し側で行う。
    /// </summary>
    public static class CRC16
    {
        public static ushort Calculate(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }

        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;

            foreach (byte value in data)
            {
                crc ^= (ushort)(value << 8);

                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }

    }
}