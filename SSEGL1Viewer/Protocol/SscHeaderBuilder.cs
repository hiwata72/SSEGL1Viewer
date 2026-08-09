namespace SSEGL1Viewer.Protocol;

/// <summary>
/// libssc.so の SSC::Transport::send() を基にした
/// SSCヘッダ生成処理。
/// </summary>
internal static class SscHeaderBuilder
{
    private const byte HeaderTerminator = 0xC9;

    /// <summary>
    /// Source Address / Destination Address の省略を表す特殊値。
    /// </summary>
    private const uint OmittedAddress = 0x08000000;

    public static byte[] Build(SscPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.PayloadLength > 0x100)
        {
            throw new NotSupportedException(
                "現在はPayload長256バイト以下のみ対応しています。");
        }

        List<byte> header = new(capacity: 20);

        byte transactionByte = packet.TransactionByte;

        // bVar3 & 0x3F
        byte requestFlags =
            (byte)(packet.RequestDefinition & 0x3F);

        /*
         * uVar13 =
         *   (((uVar14 & 0x0F) << 6) |
         *    ((uVar14 & 0x03) << 2))
         *   ^ 0xC0;
         */
        byte control = unchecked((byte)(
            ((((uint)transactionByte & 0x0F) << 6) |
             (((uint)transactionByte & 0x03) << 2))
            ^ 0xC0));

        // Packet +0x0C: Mode
        AppendMode(
            header,
            packet.Mode,
            ref requestFlags);

        // Packet +0x08: Destination Address
        if (packet.DestinationAddress != OmittedAddress)
        {
            AppendTopAlignedAddress(
                header,
                packet.DestinationAddress,
                nameof(packet.DestinationAddress));

            control |= 0x10;
        }

        // Packet +0x04: Source Address
        if (packet.SourceAddress != OmittedAddress)
        {
            AppendTopAlignedAddress(
                header,
                packet.SourceAddress,
                nameof(packet.SourceAddress));

            control |= 0x20;
        }

        // Payload Length
        if (packet.PayloadLength > 0)
        {
            /*
             * 1～256バイト:
             * encodedLength = payloadLength - 1
             *
             * 元コード:
             * (uVar21 + 0xFF) & 0xFF
             */
            byte encodedLength =
                unchecked((byte)(packet.PayloadLength - 1));

            AppendEscaped(header, encodedLength);

            control |= 0x02;
        }

        // Sequence / Transaction ID
        byte transactionId =
            (byte)((transactionByte >> 4) & 0x0F);

        byte sequence = unchecked((byte)(
            (transactionId | (transactionId << 4))
            ^ 0xF0));

        AppendEscaped(header, sequence);
        AppendEscaped(header, requestFlags);
        AppendEscaped(header, control);

        // 終端のC9はエスケープせず、そのまま追加
        header.Add(HeaderTerminator);

        return header.ToArray();
    }

    /// <summary>
    /// ModeをTransport::send()と同じリトルエンディアン順で格納します。
    /// </summary>
    private static void AppendMode(
        List<byte> output,
        uint mode,
        ref byte requestFlags)
    {
        if (mode == 0)
        {
            return;
        }

        AppendEscaped(output, (byte)mode);

        if (mode < 0x100)
        {
            requestFlags |= 0x40;
            return;
        }

        AppendEscaped(output, (byte)(mode >> 8));

        if (mode < 0x10000)
        {
            requestFlags |= 0x80;
            return;
        }

        AppendEscaped(output, (byte)(mode >> 16));
        AppendEscaped(output, (byte)(mode >> 24));

        requestFlags =
            (byte)((requestFlags & 0x3F) | 0xC0);
    }

    /// <summary>
    /// SSCの上位詰めアドレスを格納します。
    ///
    /// 例:
    /// 0x80000000 → 80
    /// 0x80800000 → 80 80
    /// 0x80808000 → 80 80 80
    /// 0x80808080 → 80 80 80 80
    /// </summary>
    private static void AppendTopAlignedAddress(
        List<byte> output,
        uint address,
        string parameterName)
    {
        if (address == 0)
        {
            return;
        }

        byte[] bytes =
        [
            (byte)address,
            (byte)(address >> 8),
            (byte)(address >> 16),
            (byte)(address >> 24)
        ];

        /*
         * 下位側のゼロを飛ばし、最初の有効バイトから
         * 最上位バイトまでを出力します。
         */
        int firstByteIndex = 0;

        while (firstByteIndex < bytes.Length &&
               bytes[firstByteIndex] == 0)
        {
            firstByteIndex++;
        }

        if (firstByteIndex >= bytes.Length)
        {
            return;
        }

        for (int index = firstByteIndex;
             index < bytes.Length;
             index++)
        {
            byte value = bytes[index];

            /*
             * SSCアドレスの各構成バイトは、
             * 最上位ビットが立っている必要があります。
             */
            if ((value & 0x80) == 0)
            {
                throw new ArgumentException(
                    $"SSCアドレス値が不正です: 0x{address:X8}",
                    parameterName);
            }

            AppendEscaped(output, value);
        }
    }

    /// <summary>
    /// データ中に0xC9が現れる場合、直前に0x00を挿入します。
    /// </summary>
    private static void AppendEscaped(
        List<byte> output,
        byte value)
    {
        if (value == HeaderTerminator)
        {
            output.Add(0x00);
        }

        output.Add(value);
    }
}