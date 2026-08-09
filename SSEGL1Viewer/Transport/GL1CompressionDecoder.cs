using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace SSEGL1Viewer.Transport
{
    public sealed class GL1CompressionDecoder
    {
        private static readonly DateTimeOffset DeviceEpoch =
            new DateTimeOffset(
                2001,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

        private readonly StreamState[] _streamStates =
        {
            new StreamState(),
            new StreamState(),
            new StreamState(),
            new StreamState()
        };

        public bool TryDecode(
            SSCNotify notify,
            out GL1DecodedPacket? packet,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(notify);

            packet = null;
            error = string.Empty;

            //byte dataId =
            //    notify.DataId;

            int dataId = notify.DataId;

            if (dataId > 3)
            {
                error =
                    $"未対応のData IDです。" +
                    $" DataId={dataId}";

                return false;
            }

            StreamState state =
                _streamStates[dataId];

            ReadOnlySpan<byte> payload =
                notify.Payload;

            int cursor = 0;

            if (!TryReadTimestamp(
                    payload,
                    ref cursor,
                    notify.DeltaTime,
                    state,
                    out ulong timestampNanoseconds,
                    out error))
            {
                return false;
            }

            bool hasSecondaryVector =
                dataId == 0 ||
                dataId == 1;

            List<GL1DecodedSample> samples;

            if (notify.Compressed)
            {
                if (!TryDecodeCompressed(
                        payload.Slice(cursor),
                        state,
                        hasSecondaryVector,
                        out samples,
                        out error))
                {
                    return false;
                }
            }
            else
            {
                if (!TryDecodeUncompressed(
                        payload.Slice(cursor),
                        hasSecondaryVector,
                        out samples,
                        out error))
                {
                    return false;
                }
            }

            packet =
                new GL1DecodedPacket
                {
                    DataId =
                        dataId,

                    IsDeltaTime =
                        notify.DeltaTime,

                    IsCompressed =
                        notify.Compressed,

                    TimestampNanoseconds =
                        timestampNanoseconds,

                    Timestamp =
                        ConvertTimestamp(
                            timestampNanoseconds),

                    Samples =
                        samples
                };

            return true;
        }

        public void Reset()
        {
            foreach (StreamState state
                     in _streamStates)
            {
                state.Reset();
            }
        }

        private static bool TryReadTimestamp(
            ReadOnlySpan<byte> payload,
            ref int cursor,
            bool isDeltaTime,
            StreamState state,
            out ulong timestampNanoseconds,
            out string error)
        {
            timestampNanoseconds = 0;
            error = string.Empty;

            if (!isDeltaTime)
            {
                if (payload.Length < 8)
                {
                    error =
                        "絶対時刻8バイトが不足しています。";

                    return false;
                }

                /*
                 * ARM64 Androidはlittle-endian。
                 */
                ulong absoluteTime =
                    BinaryPrimitives
                        .ReadUInt64LittleEndian(
                            payload.Slice(0, 8));

                cursor = 8;

                /*
                 * 純正コードは絶対時刻を受信すると
                 * 各軸の圧縮状態も初期化する。
                 */
                state.ResetChannels();
                state.TimestampNanoseconds =
                    absoluteTime;
            }
            else
            {
                if (payload.Length < 4)
                {
                    error =
                        "差分時刻4バイトが不足しています。";

                    return false;
                }

                if (!state.HasTimestamp)
                {
                    error =
                        "絶対時刻を受信する前に" +
                        "差分時刻が届きました。";

                    return false;
                }

                uint deltaTime =
                    BinaryPrimitives
                        .ReadUInt32LittleEndian(
                            payload.Slice(0, 4));

                cursor = 4;

                state.TimestampNanoseconds =
                    unchecked(
                        state.TimestampNanoseconds +
                        deltaTime);
            }

            state.HasTimestamp = true;

            timestampNanoseconds =
                state.TimestampNanoseconds;

            return true;
        }

        private static bool TryDecodeCompressed(
            ReadOnlySpan<byte> compressedData,
            StreamState state,
            bool hasSecondaryVector,
            out List<GL1DecodedSample> samples,
            out string error)
        {
            samples = new List<GL1DecodedSample>();
            error = string.Empty;

            if (compressedData.Length < 1)
            {
                error =
                    "圧縮サンプル数がありません。";

                return false;
            }

            int sampleCount =
                compressedData[0] + 1;

            var reader =
                new GL1BitReader(
                    compressedData.Slice(1));

            samples =
                new List<GL1DecodedSample>(
                    sampleCount);

            for (int sampleIndex = 0;
                 sampleIndex < sampleCount;
                 sampleIndex++)
            {
                if (!TryDecodeVector(
                        ref reader,
                        state.Channels,
                        channelOffset: 0,
                        out MotionVector3 primary,
                        out error))
                {
                    error =
                        $"Primary展開失敗。" +
                        $" Sample={sampleIndex}," +
                        $" {error}";

                    return false;
                }

                MotionVector3? secondary = null;

                if (hasSecondaryVector)
                {
                    if (!TryDecodeVector(
                            ref reader,
                            state.Channels,
                            channelOffset: 3,
                            out MotionVector3 secondaryValue,
                            out error))
                    {
                        error =
                            $"Secondary展開失敗。" +
                            $" Sample={sampleIndex}," +
                            $" {error}";

                        return false;
                    }

                    secondary =
                        secondaryValue;
                }

                samples.Add(
                    new GL1DecodedSample
                    {
                        Index =
                            (uint)sampleIndex,

                        Primary =
                            primary,

                        Secondary =
                            secondary
                    });
            }

            return true;
        }

        private static bool TryDecodeVector(
            ref GL1BitReader reader,
            GL1AdaptiveChannelDecoder[] channels,
            int channelOffset,
            out MotionVector3 vector,
            out string error)
        {
            vector = default;
            error = string.Empty;

            if (!channels[channelOffset].TryGet(
                    ref reader,
                    out int x,
                    out error))
            {
                return false;
            }

            if (!channels[channelOffset + 1].TryGet(
                    ref reader,
                    out int y,
                    out error))
            {
                return false;
            }

            if (!channels[channelOffset + 2].TryGet(
                    ref reader,
                    out int z,
                    out error))
            {
                return false;
            }

            vector =
                new MotionVector3(
                    x,
                    y,
                    z);

            return true;
        }

        private static bool TryDecodeUncompressed(
            ReadOnlySpan<byte> rawData,
            bool hasSecondaryVector,
            out List<GL1DecodedSample> samples,
            out string error)
        {
            samples = new List<GL1DecodedSample>();
            error = string.Empty;

            int bytesPerSample =
                hasSecondaryVector
                    ? 12
                    : 6;

            if (rawData.Length %
                bytesPerSample != 0)
            {
                error =
                    $"非圧縮データ長が不正です。" +
                    $" Length={rawData.Length}," +
                    $" BytesPerSample={bytesPerSample}";

                return false;
            }

            int sampleCount =
                rawData.Length /
                bytesPerSample;

            samples =
                new List<GL1DecodedSample>(
                    sampleCount);

            int cursor = 0;

            for (int sampleIndex = 0;
                 sampleIndex < sampleCount;
                 sampleIndex++)
            {
                MotionVector3 primary =
                    ReadRawVector(
                        rawData,
                        ref cursor);

                MotionVector3? secondary =
                    null;

                if (hasSecondaryVector)
                {
                    secondary =
                        ReadRawVector(
                            rawData,
                            ref cursor);
                }

                samples.Add(
                    new GL1DecodedSample
                    {
                        Index =
                            (uint)sampleIndex,

                        Primary =
                            primary,

                        Secondary =
                            secondary
                    });
            }

            return true;
        }

        private static MotionVector3 ReadRawVector(
            ReadOnlySpan<byte> data,
            ref int cursor)
        {
            short x =
                BinaryPrimitives
                    .ReadInt16LittleEndian(
                        data.Slice(cursor, 2));

            cursor += 2;

            short y =
                BinaryPrimitives
                    .ReadInt16LittleEndian(
                        data.Slice(cursor, 2));

            cursor += 2;

            short z =
                BinaryPrimitives
                    .ReadInt16LittleEndian(
                        data.Slice(cursor, 2));

            cursor += 2;

            return
                new MotionVector3(
                    x,
                    y,
                    z);
        }

        private static DateTimeOffset ConvertTimestamp(
            ulong timestampNanoseconds)
        {
            /*
             * DateTimeの分解能は100ns。
             */
            ulong ticks =
                timestampNanoseconds / 100;

            if (ticks >
                (ulong)(DateTimeOffset.MaxValue -
                        DeviceEpoch).Ticks)
            {
                return DeviceEpoch;
            }

            return
                DeviceEpoch.AddTicks(
                    (long)ticks);
        }

        private sealed class StreamState
        {
            public ulong TimestampNanoseconds;

            public bool HasTimestamp;

            public GL1AdaptiveChannelDecoder[] Channels
            { get; } =
            {
                new GL1AdaptiveChannelDecoder(),
                new GL1AdaptiveChannelDecoder(),
                new GL1AdaptiveChannelDecoder(),
                new GL1AdaptiveChannelDecoder(),
                new GL1AdaptiveChannelDecoder(),
                new GL1AdaptiveChannelDecoder()
            };

            public void ResetChannels()
            {
                foreach (
                    GL1AdaptiveChannelDecoder channel
                    in Channels)
                {
                    channel.Reset();
                }
            }

            public void Reset()
            {
                TimestampNanoseconds = 0;
                HasTimestamp = false;
                ResetChannels();
            }
        }
    }
}