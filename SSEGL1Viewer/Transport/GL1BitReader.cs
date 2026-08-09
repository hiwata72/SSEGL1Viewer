using System;

namespace SSEGL1Viewer.Transport
{
    internal ref struct GL1BitReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _bitPosition;

        public GL1BitReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _bitPosition = 0;
        }

        public int RemainingBits =>
            _data.Length * 8 - _bitPosition;

        public bool TryReadBits(
            int bitCount,
            out uint value)
        {
            value = 0;

            if (bitCount < 0 || bitCount > 32)
            {
                return false;
            }

            if (bitCount == 0)
            {
                return true;
            }

            if (RemainingBits < bitCount)
            {
                return false;
            }

            for (int index = 0;
                 index < bitCount;
                 index++)
            {
                int absoluteBit =
                    _bitPosition + index;

                int byteIndex =
                    absoluteBit / 8;

                int bitInByte =
                    7 - absoluteBit % 8;

                value =
                    (value << 1) |
                    (uint)((_data[byteIndex] >> bitInByte) & 1);
            }

            _bitPosition += bitCount;

            return true;
        }

        /// <summary>
        /// 1が連続し、最初の0で終わるUnary値を読みます。
        /// 例：0→0、10→1、110→2。
        /// </summary>
        public bool TryReadUnaryOnes(
            int maximumOnes,
            out int ones)
        {
            ones = 0;

            while (ones <= maximumOnes)
            {
                if (!TryReadBits(
                        1,
                        out uint bit))
                {
                    return false;
                }

                if (bit == 0)
                {
                    return true;
                }

                ones++;

                if (ones > maximumOnes)
                {
                    return false;
                }
            }

            return false;
        }
    }
}