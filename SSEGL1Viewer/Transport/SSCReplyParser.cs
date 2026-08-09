using System;

namespace SSEGL1Viewer.Transport
{
    public static class SSCReplyParser
    {
        private const byte HeaderDelimiter = 0xC9;
        private const uint DefaultAddress = 0x08000000;

        public static bool TryParse(
            ReadOnlySpan<byte> data,
            out SSCReply? reply,
            out string error)
        {
            reply = null;
            error = string.Empty;

            int delimiterIndex = data.IndexOf(HeaderDelimiter);

            if (delimiterIndex < 0)
            {
                error = "ヘッダー終端0xC9がありません。";
                return false;
            }

            if (delimiterIndex < 3)
            {
                error = "SSCヘッダーが短すぎます。";
                return false;
            }

            byte flags = data[delimiterIndex - 1];
            byte commandByte = data[delimiterIndex - 2];
            byte tidByte = data[delimiterIndex - 3];

            byte tid = (byte)(tidByte & 0x0F);
            byte tidComplement = (byte)(tidByte >> 4);

            if (tidComplement != (byte)(tid ^ 0x0F))
            {
                error =
                    $"TID補数が不正です。TID byte=0x{tidByte:X2}";

                return false;
            }

            byte category = (byte)((flags >> 2) & 0x03);
            byte command = (byte)(commandByte & 0x3F);
            byte lengthMode = (byte)(flags & 0x03);

            // Command上位2bitはAR長。
            // 現在はARなしにだけ対応。
            int arLength = commandByte >> 6;

            if (arLength != 0)
            {
                error =
                    $"AR付き応答はまだ未対応です。AR長={arLength}";

                return false;
            }

            int cursor = delimiterIndex - 4;
            int payloadLength;

            switch (lengthMode)
            {
                case 0:
                    payloadLength = 0;
                    break;

                case 2:
                    if (cursor < 0)
                    {
                        error = "Payload長フィールドが不足しています。";
                        return false;
                    }

                    // Sony形式：
                    // encodedLength = payloadLength - 1
                    payloadLength = data[cursor] + 1;
                    cursor--;
                    break;

                case 1:
                    error = "LengthMode 1はまだ未対応です。";
                    return false;

                case 3:
                    error = "拡張Payload長形式はまだ未対応です。";
                    return false;

                default:
                    error = "不正なLengthModeです。";
                    return false;
            }

            uint sourceAddress = DefaultAddress;
            uint destinationAddress = DefaultAddress;

            bool hasSourceAddress = (flags & 0x20) != 0;
            bool hasDestinationAddress = (flags & 0x10) != 0;

            if (hasSourceAddress)
            {
                if (cursor < 0)
                {
                    error = "SAフィールドが不足しています。";
                    return false;
                }

                // 現在は1バイト表現に対応
                sourceAddress = (uint)data[cursor] << 24;
                cursor--;
            }

            if (hasDestinationAddress)
            {
                if (cursor < 0)
                {
                    error = "DAフィールドが不足しています。";
                    return false;
                }

                destinationAddress = (uint)data[cursor] << 24;
                cursor--;
            }

            //int payloadStart = delimiterIndex + 1;
            //int crcStart = payloadStart + payloadLength;

            //if (data.Length < crcStart + 2)
            //{
            //    error =
            //        $"PayloadまたはCRCが不足しています。" +
            //        $"必要={crcStart + 2}, 実際={data.Length}";

            //    return false;
            //}

            //byte[] payload =
            //    data.Slice(payloadStart, payloadLength).ToArray();

            //ushort receivedCrc =
            //    (ushort)((data[crcStart] << 8) |
            //              data[crcStart + 1]);

            //ushort calculatedCrc =
            //    CRC16.Calculate(payload);

            //if (receivedCrc != calculatedCrc)
            //{
            //    error =
            //        $"CRC不一致です。" +
            //        $"受信=0x{receivedCrc:X4}, " +
            //        $"計算=0x{calculatedCrc:X4}";

            //    return false;
            //}

            int payloadStart = delimiterIndex + 1;

            if (data.Length < payloadStart + payloadLength)
            {
                error =
                    $"Payloadが不足しています。" +
                    $"必要={payloadStart + payloadLength}, " +
                    $"実際={data.Length}";

                return false;
            }

            byte[] payload =
                data.Slice(
                    payloadStart,
                    payloadLength)
                .ToArray();

            // PayloadなしreplyはC9で終了し、CRCが付かない。
            if (payloadLength > 0)
            {
                int crcStart =
                    payloadStart + payloadLength;

                if (data.Length < crcStart + 2)
                {
                    error =
                        $"CRCが不足しています。" +
                        $"必要={crcStart + 2}, " +
                        $"実際={data.Length}";

                    return false;
                }

                ushort receivedCrc =
                    (ushort)(
                        (data[crcStart] << 8) |
                         data[crcStart + 1]);

                ushort calculatedCrc =
                    CRC16.Calculate(payload);

                if (receivedCrc != calculatedCrc)
                {
                    error =
                        $"CRC不一致です。" +
                        $"受信=0x{receivedCrc:X4}, " +
                        $"計算=0x{calculatedCrc:X4}";

                    return false;
                }
            }

            reply = new SSCReply
            {
                Tid = tid,
                Command = command,
                Category = category,
                Flags = flags,
                SourceAddress = sourceAddress,
                DestinationAddress = destinationAddress,
                Payload = payload
            };

            return true;
        }
    }
}