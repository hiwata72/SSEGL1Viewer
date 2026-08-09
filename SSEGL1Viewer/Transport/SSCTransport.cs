using System;
using System.Collections.Generic;
using System.Text;

namespace SSEGL1Viewer.Transport
{
    public static class SSCTransport
    {
        public static byte[] BuildFrame(byte tid, byte command, byte[] payload, byte destinationAddress)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var frame = new List<byte>();

            frame.AddRange(
                BuildHeader(
                    tid,
                    command,
                    payload,
                    destinationAddress));

            frame.AddRange(payload);
            frame.AddRange(BuildTrailer(payload));

            return frame.ToArray();
        }

        public static byte[] BuildFrame(byte tid, byte command, byte[] payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var frame = new List<byte>();

            frame.AddRange(
                BuildHeader(
                    tid,
                    command,
                    payload));

            frame.AddRange(payload);
            frame.AddRange(BuildTrailer(payload));

            return frame.ToArray();
        }

        public static byte[] BuildFrameWithArgument(
            byte tid,
            byte command,
            uint argument,
            int argumentLength,
            byte[] payload,
            byte destinationAddress)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var frame = new List<byte>();

            frame.AddRange(
                BuildHeaderWithArgument(
                    tid,
                    command,
                    argument,
                    argumentLength,
                    payload,
                    destinationAddress));

            frame.AddRange(payload);
            frame.AddRange(BuildTrailer(payload));

            return frame.ToArray();
        }

        private static byte[] BuildHeader(byte tid, byte command, ReadOnlySpan<byte> payload, byte? destinationAddress = null)
        {
            var header = new List<byte>();

            if (payload.Length > 256)
            {
                throw new NotSupportedException(
                    "256バイトを超えるPayloadにはまだ対応していません。");
            }

            byte flags = 0xC0;

            // DAを明示する場合
            if (destinationAddress.HasValue)
            {
                header.Add(destinationAddress.Value);
                flags |= 0x10;
            }

            // Payload長
            if (payload.Length > 0)
            {
                header.Add((byte)(payload.Length - 1));
                flags |= 0x02;
            }

            byte normalizedTid = (byte)(tid & 0x0F);

            byte tidByte = (byte)(
                (((~normalizedTid) & 0x0F) << 4) |
                normalizedTid);

            header.Add(tidByte);
            header.Add((byte)(command & 0x3F));
            header.Add(flags);
            header.Add(0xC9);

            return header.ToArray();
        }

        private static byte[] BuildHeaderWithArgument(
            byte tid,
            byte command,
            uint argument,
            int argumentLength,
            ReadOnlySpan<byte> payload,
            byte destinationAddress)
        {
            if (payload.Length > 256)
            {
                throw new NotSupportedException(
                    "256バイトを超えるPayloadにはまだ対応していません。");
            }

            int argumentLengthMode =
                argumentLength switch
                {
                    1 => 1,
                    2 => 2,
                    4 => 3,

                    _ => throw new ArgumentOutOfRangeException(
                        nameof(argumentLength),
                        argumentLength,
                        "Argument長は1、2、4バイトを指定してください。")
                };

            var header = new List<byte>();

            // 1. Argument
            // 純正Transportは下位バイトから格納する。
            for (int shift = 0;
                 shift < argumentLength * 8;
                 shift += 8)
            {
                header.Add(
                    (byte)(argument >> shift));
            }

            // 2. Destination Address
            header.Add(destinationAddress);

            byte flags = 0xC0;

            // DAあり
            flags |= 0x10;

            // 3. Payload長
            if (payload.Length > 0)
            {
                header.Add(
                    (byte)(payload.Length - 1));

                flags |= 0x02;
            }

            byte normalizedTid =
                (byte)(tid & 0x0F);

            byte tidByte =
                (byte)(
                    (((~normalizedTid) & 0x0F) << 4) |
                     normalizedTid);

            byte commandByte =
                (byte)(
                    ((argumentLengthMode & 0x03) << 6) |
                     (command & 0x3F));

            header.Add(tidByte);
            header.Add(commandByte);
            header.Add(flags);
            header.Add(0xC9);

            return header.ToArray();
        }
        private static byte[] BuildTrailer(
            ReadOnlySpan<byte> payload)
        {
            var trailer = new List<byte>();

            int paddingLength = (-payload.Length) & 3;

            for (int i = 0; i < paddingLength; i++)
            {
                trailer.Add(0x00);
            }

            ushort crc = CRC16.Compute(payload);

            // バイト順は暫定
            trailer.Add((byte)(crc >> 8));
            trailer.Add((byte)(crc & 0xFF));

            return trailer.ToArray();
        }

        public static byte[] BuildDeviceModelPayload()
        {
            return BuildStringPropertyPayload("device.model");
        }

        public static byte[] BuildStringPropertyPayload(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException(
                    "プロパティ名が空です。",
                    nameof(propertyName));
            }

            if (propertyName.Contains('\0'))
            {
                throw new ArgumentException(
                    "プロパティ名にNUL文字は使用できません。",
                    nameof(propertyName));
            }

            return System.Text.Encoding.ASCII.GetBytes(
                propertyName + "\0");
        }
    }
}