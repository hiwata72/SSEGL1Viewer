using System;

namespace SSEGL1Viewer.Transport
{
    public static class SSCNotifyParser
    {
        private const byte HeaderDelimiter = 0xC9;
        private const uint DefaultAddress = 0x08000000;

        public static bool TryParse(
            ReadOnlySpan<byte> data,
            out SSCNotify? notify,
            out string error)
        {
            notify = null;
            error = string.Empty;

            int delimiterIndex =
                data.IndexOf(HeaderDelimiter);

            if (delimiterIndex < 0)
            {
                error =
                    "ヘッダー終端0xC9がありません。";

                return false;
            }

            /*
             * 最低限必要なヘッダー：
             *
             * TID
             * Command
             * Flags
             * C9
             */
            if (delimiterIndex < 3)
            {
                error =
                    "SSCヘッダーが短すぎます。";

                return false;
            }

            byte flags =
                data[delimiterIndex - 1];

            byte commandByte =
                data[delimiterIndex - 2];

            byte tidByte =
                data[delimiterIndex - 3];

            /*
             * TID byte：
             *
             * 上位4bit = 下位4bitの反転値
             * 下位4bit = TID
             */
            byte tid =
                (byte)(tidByte & 0x0F);

            byte tidComplement =
                (byte)(tidByte >> 4);

            if (tidComplement !=
                (byte)(tid ^ 0x0F))
            {
                error =
                    $"TID補数が不正です。" +
                    $"TID byte=0x{tidByte:X2}";

                return false;
            }

            byte category =
                (byte)((flags >> 2) & 0x03);

            if (category != 0x01)
            {
                error =
                    $"Notifyではありません。" +
                    $"Category={category}";

                return false;
            }

            byte command =
                (byte)(commandByte & 0x3F);

            byte lengthMode =
                (byte)(flags & 0x03);

            bool hasSourceAddress =
                (flags & 0x20) != 0;

            bool hasDestinationAddress =
                (flags & 0x10) != 0;

            /*
             * C9の直前は次の順：
             *
             * TID
             * Command
             * Flags
             * C9
             *
             * delimiterIndex - 4 から、
             * さらに前の可変フィールドを逆向きに読みます。
             */
            int cursor =
                delimiterIndex - 4;

            /*
             * 1. Payload長
             */
            if (!TryReadPayloadLength(
                    data,
                    lengthMode,
                    ref cursor,
                    out int payloadLength,
                    out error))
            {
                return false;
            }

            /*
             * 2. Source Address
             *
             * ヘッダーを逆向きに読むため、
             * SAをDAより先に処理します。
             */
            uint sourceAddress =
                DefaultAddress;

            if (hasSourceAddress)
            {
                if (cursor < 0)
                {
                    error =
                        "SAフィールドが不足しています。";

                    return false;
                }

                // 現在は1バイト短縮表現に対応。
                sourceAddress =
                    (uint)data[cursor] << 24;

                cursor--;
            }

            /*
             * 3. Destination Address
             */
            uint destinationAddress =
                DefaultAddress;

            if (hasDestinationAddress)
            {
                if (cursor < 0)
                {
                    error =
                        "DAフィールドが不足しています。";

                    return false;
                }

                // 現在は1バイト短縮表現に対応。
                destinationAddress =
                    (uint)data[cursor] << 24;

                cursor--;
            }

            /*
             * 4. Argument
             *
             * Command上位2bit：
             *
             * 00 = Argumentなし
             * 01 = 1 byte
             * 10 = 2 bytes
             * 11 = 4 bytes
             */
            int argumentLength =
                GetArgumentLength(commandByte);

            uint argument = 0;

            if (argumentLength > 0)
            {
                int argumentStart =
                    cursor - argumentLength + 1;

                if (argumentStart < 0)
                {
                    error =
                        $"ARフィールドが不足しています。" +
                        $"AR長={argumentLength}";

                    return false;
                }

                /*
                 * 純正TransportはArgumentを
                 * 下位バイトから順に格納します。
                 */
                for (int index = 0;
                     index < argumentLength;
                     index++)
                {
                    argument |=
                        (uint)data[argumentStart + index]
                        << (index * 8);
                }

                cursor -= argumentLength;
            }

            /*
             * 可変ヘッダーをすべて読み終えた時点で、
             * cursorは通常 -1 になります。
             *
             * 余分な先行バイトがある場合でも、
             * 現段階ではエラーにせず解析を続けます。
             */

            int payloadStart =
                delimiterIndex + 1;

            int payloadEnd =
                payloadStart + payloadLength;

            if (data.Length < payloadEnd)
            {
                error =
                    $"Payloadが不足しています。" +
                    $"必要={payloadEnd}, " +
                    $"実際={data.Length}";

                return false;
            }

            byte[] payload =
                data.Slice(
                    payloadStart,
                    payloadLength)
                .ToArray();

            /*
             * Payloadの後は4バイト境界までPaddingされ、
             * その後にCRC 2バイトが続きます。
             */
            // 受信NotifyではPayload直後にCRCが続く。
            // Paddingは付かない。
            int paddingLength = 0;

            int crcStart =
                payloadEnd;

            int frameLength =
                crcStart + 2;

            if (data.Length < frameLength)
            {
                error =
                    $"CRCが不足しています。" +
                    $"必要={frameLength}, " +
                    $"実際={data.Length}, " +
                    $"Payload長={payloadLength}";

                return false;
            }

            /*
             * Paddingは通常0x00。
             * 現段階では値が0以外でも解析を止めず、
             * CRC位置の決定だけに使用します。
             */
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
                    $"計算=0x{calculatedCrc:X4}, " +
                    $"Payload長={payloadLength}, " +
                    $"Padding長={paddingLength}";

                return false;
            }

            notify = new SSCNotify
            {
                Tid = tid,
                Category = category,
                Command = command,
                Flags = flags,
                SourceAddress = sourceAddress,
                DestinationAddress = destinationAddress,
                Argument = argument,
                Payload = payload
            };

            return true;
        }

        private static bool TryReadPayloadLength(
            ReadOnlySpan<byte> data,
            byte lengthMode,
            ref int cursor,
            out int payloadLength,
            out string error)
        {
            payloadLength = 0;
            error = string.Empty;

            switch (lengthMode)
            {
                case 0:
                    payloadLength = 0;
                    return true;

                case 1:
                    error =
                        "LengthMode 1は未対応です。";

                    return false;

                case 2:
                    if (cursor < 0)
                    {
                        error =
                            "Payload長フィールドが不足しています。";

                        return false;
                    }

                    /*
                     * 1バイト長形式：
                     *
                     * encodedLength =
                     *     payloadLength - 1
                     */
                    payloadLength =
                        data[cursor] + 1;

                    cursor--;

                    return true;

                case 3:
                    error =
                        "拡張Payload長形式は未対応です。";

                    return false;

                default:
                    error =
                        $"不正なLengthModeです。" +
                        $"LengthMode={lengthMode}";

                    return false;
            }
        }

        private static int GetArgumentLength(
            byte commandByte)
        {
            int argumentLengthMode =
                commandByte >> 6;

            return argumentLengthMode switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 4,

                _ => throw new InvalidOperationException(
                    "不正なAR長モードです。")
            };
        }
    }
}