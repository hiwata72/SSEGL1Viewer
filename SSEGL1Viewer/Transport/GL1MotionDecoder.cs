using System;
using System.Collections.Generic;

namespace SSEGL1Viewer.Transport
{
    public static class GL1MotionDecoder
    {
        private const int HeaderLength = 16;

        private const byte AccelerationFlag = 0x01;
        private const byte GyroscopeFlag = 0x02;
        private const byte UnknownVectorFlag = 0x04;
        private const byte FeatureFlag = 0x08;

        public static bool TryDecode(
            ReadOnlySpan<byte> payload,
            out GL1MotionPacket? packet,
            out string error)
        {
            packet = null;
            error = string.Empty;

            if (payload.Length < HeaderLength)
            {
                error =
                    $"Motion Payloadが短すぎます。" +
                    $"必要={HeaderLength}, " +
                    $"実際={payload.Length}";

                return false;
            }

            int year = 2000 + payload[0];
            int month = payload[1];
            int day = payload[2];
            int hour = payload[3];
            int minute = payload[4];
            int second = payload[5];

            ushort milliseconds =
                ReadUInt16BigEndian(payload, 6);

            uint startIndex =
                ((uint)payload[8] << 16) |
                ((uint)payload[9] << 8) |
                 payload[10];

            byte sampleCount = payload[11];
            byte channelFlags = payload[12];

            ushort samplingFrequency =
                ReadUInt16BigEndian(payload, 13);

            byte quantizationMode = payload[15];

            if (!TryCreateTimestamp(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    milliseconds,
                    out DateTime baseTimestamp,
                    out error))
            {
                return false;
            }

            if (!TryGetChannelWidths(
                    channelFlags,
                    quantizationMode,
                    out int accelerationWidth,
                    out int gyroscopeWidth,
                    out int unknownVectorWidth,
                    out int featureWidth,
                    out error))
            {
                return false;
            }

            int bytesPerSample =
                accelerationWidth * 3 +
                gyroscopeWidth * 3 +
                unknownVectorWidth * 3 +
                featureWidth * 4;

            int expectedDataLength =
                bytesPerSample * sampleCount;

            int actualDataLength =
                payload.Length - HeaderLength;

            if (actualDataLength != expectedDataLength)
            {
                error =
                    $"Motionサンプル長が一致しません。" +
                    $" SampleCount={sampleCount}," +
                    $" 1サンプル={bytesPerSample} bytes," +
                    $" 必要={expectedDataLength}," +
                    $" 実際={actualDataLength}," +
                    $" Flags=0x{channelFlags:X2}," +
                    $" QuantMode={quantizationMode}";

                return false;
            }

            var samples =
                new List<MotionSample>(sampleCount);

            int cursor = HeaderLength;

            for (int sampleOffset = 0;
                 sampleOffset < sampleCount;
                 sampleOffset++)
            {
                MotionVector3? acceleration = null;
                MotionVector3? gyroscope = null;

                if (accelerationWidth > 0)
                {
                    if (!TryReadVector3(
                            payload,
                            ref cursor,
                            accelerationWidth,
                            out MotionVector3 value,
                            out error))
                    {
                        return false;
                    }

                    acceleration = value;
                }

                if (gyroscopeWidth > 0)
                {
                    if (!TryReadVector3(
                            payload,
                            ref cursor,
                            gyroscopeWidth,
                            out MotionVector3 value,
                            out error))
                    {
                        return false;
                    }

                    gyroscope = value;
                }

                /*
                 * ChannelFlags bit2。
                 * 純正コードでも値を保存せず、
                 * サイズ分だけ読み飛ばしています。
                 */
                if (unknownVectorWidth > 0)
                {
                    int skipLength =
                        unknownVectorWidth * 3;

                    if (!TrySkip(
                            payload,
                            ref cursor,
                            skipLength,
                            out error))
                    {
                        return false;
                    }
                }

                /*
                 * ChannelFlags bit3。
                 * 純正コードでは4成分として扱っています。
                 * 意味が確定するまでは読み飛ばします。
                 */
                if (featureWidth > 0)
                {
                    int skipLength =
                        featureWidth * 4;

                    if (!TrySkip(
                            payload,
                            ref cursor,
                            skipLength,
                            out error))
                    {
                        return false;
                    }
                }

                DateTime sampleTimestamp =
                    CalculateSampleTimestamp(
                        baseTimestamp,
                        sampleOffset,
                        samplingFrequency);

                samples.Add(
                    new MotionSample
                    {
                        Index =
                            startIndex +
                            (uint)sampleOffset,

                        Timestamp =
                            sampleTimestamp,

                        Acceleration =
                            acceleration,

                        Gyroscope =
                            gyroscope,

                        HasUnknownVectorChannel =
                            unknownVectorWidth > 0,

                        HasFeatureChannel =
                            featureWidth > 0
                    });
            }

            if (cursor != payload.Length)
            {
                error =
                    $"Motion Payloadの終端位置が一致しません。" +
                    $"解析位置={cursor}, " +
                    $"Payload長={payload.Length}";

                return false;
            }

            packet =
                new GL1MotionPacket
                {
                    BaseTimestamp =
                        baseTimestamp,

                    Milliseconds =
                        milliseconds,

                    StartIndex =
                        startIndex,

                    SampleCount =
                        sampleCount,

                    ChannelFlags =
                        channelFlags,

                    SamplingFrequency =
                        samplingFrequency,

                    QuantizationMode =
                        quantizationMode,

                    Samples =
                        samples
                };

            return true;
        }

        private static bool TryGetChannelWidths(
            byte channelFlags,
            byte quantizationMode,
            out int accelerationWidth,
            out int gyroscopeWidth,
            out int unknownVectorWidth,
            out int featureWidth,
            out string error)
        {
            error = string.Empty;

            bool hasAcceleration =
                (channelFlags & AccelerationFlag) != 0;

            bool hasGyroscope =
                (channelFlags & GyroscopeFlag) != 0;

            bool hasUnknownVector =
                (channelFlags & UnknownVectorFlag) != 0;

            bool hasFeature =
                (channelFlags & FeatureFlag) != 0;

            /*
             * 純正コードの挙動：
             *
             * QuantMode 0:
             *   Accel/Gyro/Unknown = 1 byte
             *   Feature           = 1 byte × 4
             *
             * QuantMode 1:
             *   Accel/Gyro/Unknown = 1 byte
             *   Feature           = 2 bytes × 4
             *
             * QuantMode 2:
             *   Accel/Gyro/Unknown = 2 bytes
             *   Feature           = 2 bytes × 4
             */
            switch (quantizationMode)
            {
                case 0:
                    accelerationWidth =
                        hasAcceleration ? 1 : 0;

                    gyroscopeWidth =
                        hasGyroscope ? 1 : 0;

                    unknownVectorWidth =
                        hasUnknownVector ? 1 : 0;

                    featureWidth =
                        hasFeature ? 1 : 0;

                    return true;

                case 1:
                    accelerationWidth =
                        hasAcceleration ? 1 : 0;

                    gyroscopeWidth =
                        hasGyroscope ? 1 : 0;

                    unknownVectorWidth =
                        hasUnknownVector ? 1 : 0;

                    featureWidth =
                        hasFeature ? 2 : 0;

                    return true;

                case 2:
                    accelerationWidth =
                        hasAcceleration ? 2 : 0;

                    gyroscopeWidth =
                        hasGyroscope ? 2 : 0;

                    unknownVectorWidth =
                        hasUnknownVector ? 2 : 0;

                    featureWidth =
                        hasFeature ? 2 : 0;

                    return true;

                default:
                    accelerationWidth = 0;
                    gyroscopeWidth = 0;
                    unknownVectorWidth = 0;
                    featureWidth = 0;

                    error =
                        $"未対応のQuantModeです。" +
                        $" QuantMode={quantizationMode}";

                    return false;
            }
        }

        private static bool TryReadVector3(
            ReadOnlySpan<byte> payload,
            ref int cursor,
            int componentWidth,
            out MotionVector3 value,
            out string error)
        {
            value = default;
            error = string.Empty;

            if (!TryReadSignedValue(
                    payload,
                    ref cursor,
                    componentWidth,
                    out int x,
                    out error))
            {
                return false;
            }

            if (!TryReadSignedValue(
                    payload,
                    ref cursor,
                    componentWidth,
                    out int y,
                    out error))
            {
                return false;
            }

            if (!TryReadSignedValue(
                    payload,
                    ref cursor,
                    componentWidth,
                    out int z,
                    out error))
            {
                return false;
            }

            value =
                new MotionVector3(
                    x,
                    y,
                    z);

            return true;
        }

        private static bool TryReadSignedValue(
            ReadOnlySpan<byte> payload,
            ref int cursor,
            int valueWidth,
            out int value,
            out string error)
        {
            value = 0;
            error = string.Empty;

            switch (valueWidth)
            {
                case 1:
                    if (cursor >= payload.Length)
                    {
                        error =
                            "8bit Motion値が不足しています。";

                        return false;
                    }

                    value =
                        unchecked((sbyte)payload[cursor]);

                    cursor++;

                    return true;

                case 2:
                    if (cursor + 1 >= payload.Length)
                    {
                        error =
                            "16bit Motion値が不足しています。";

                        return false;
                    }

                    short signedValue =
                        unchecked(
                            (short)(
                                (payload[cursor] << 8) |
                                 payload[cursor + 1]));

                    value =
                        signedValue;

                    cursor += 2;

                    return true;

                default:
                    error =
                        $"未対応の値幅です。" +
                        $" Width={valueWidth}";

                    return false;
            }
        }

        private static bool TrySkip(
            ReadOnlySpan<byte> payload,
            ref int cursor,
            int count,
            out string error)
        {
            error = string.Empty;

            if (count < 0 ||
                cursor + count > payload.Length)
            {
                error =
                    $"Motionデータの読み飛ばし範囲が不正です。" +
                    $" Cursor={cursor}," +
                    $" Count={count}," +
                    $" PayloadLength={payload.Length}";

                return false;
            }

            cursor += count;

            return true;
        }

        private static ushort ReadUInt16BigEndian(
            ReadOnlySpan<byte> data,
            int offset)
        {
            return
                (ushort)(
                    (data[offset] << 8) |
                     data[offset + 1]);
        }

        private static bool TryCreateTimestamp(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            int millisecond,
            out DateTime timestamp,
            out string error)
        {
            timestamp = default;
            error = string.Empty;

            try
            {
                timestamp =
                    new DateTime(
                        year,
                        month,
                        day,
                        hour,
                        minute,
                        second,
                        millisecond,
                        DateTimeKind.Unspecified);

                return true;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                error =
                    $"Motion日時が不正です。" +
                    $" {year:D4}-{month:D2}-{day:D2}" +
                    $" {hour:D2}:{minute:D2}:{second:D2}" +
                    $".{millisecond:D3}" +
                    $" ({ex.Message})";

                return false;
            }
        }

        private static DateTime CalculateSampleTimestamp(
            DateTime baseTimestamp,
            int sampleOffset,
            ushort samplingFrequency)
        {
            if (samplingFrequency == 0)
            {
                return baseTimestamp;
            }

            double elapsedSeconds =
                (double)sampleOffset /
                samplingFrequency;

            return baseTimestamp.AddSeconds(
                elapsedSeconds);
        }
    }
}