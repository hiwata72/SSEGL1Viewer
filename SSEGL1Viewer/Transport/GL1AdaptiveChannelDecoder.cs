using System;
using System.Numerics;

namespace SSEGL1Viewer.Transport
{
    internal sealed class GL1AdaptiveChannelDecoder
    {
        private int _state;
        private int _rank;
        private int _previousValue;

        public void Reset()
        {
            _state = 0;
            _rank = 0;
            _previousValue = 0;
        }

        public bool TryGet(
            ref GL1BitReader reader,
            out int value,
            out string error)
        {
            value = 0;
            error = string.Empty;

            /*
             * 初回値：
             * 5bitで値幅を取得し、
             * その値幅で符号付き値を読む。
             */
            if (_state == 0)
            {
                if (!TryReadWidth(
                        ref reader,
                        out int width,
                        out error))
                {
                    return false;
                }

                if (!TryReadSigned(
                        ref reader,
                        width,
                        out int initialValue,
                        out error))
                {
                    return false;
                }

                _previousValue =
                    initialValue;

                _state = 1;
                value = _previousValue;

                return true;
            }

            /*
             * 2番目の値：
             * 5bitで初期rankを取得し、
             * 差分を前回値へ加算する。
             */
            if (_state == 1)
            {
                if (!TryReadWidth(
                        ref reader,
                        out int width,
                        out error))
                {
                    return false;
                }

                if (!TryReadSigned(
                        ref reader,
                        width,
                        out int difference,
                        out error))
                {
                    return false;
                }

                _rank = width;
                _previousValue =
                    unchecked(
                        _previousValue +
                        difference);

                _state = 2;
                value = _previousValue;

                return true;
            }

            /*
             * 通常状態。
             *
             * 0      : 現在rankで差分
             * 10     : rank+1で差分
             * 110    : rank+2で差分
             * ...
             */
            if (!reader.TryReadUnaryOnes(
                    maximumOnes: 23,
                    out int rankIncrease))
            {
                error =
                    "圧縮データのUnaryコードが不足しています。";

                return false;
            }

            int valueWidth;

            if (rankIncrease == 0)
            {
                valueWidth = _rank;
            }
            else
            {
                valueWidth =
                    _rank + rankIncrease;
            }

            if (valueWidth < 0 ||
                valueWidth > 31)
            {
                error =
                    $"圧縮値幅が範囲外です。" +
                    $" Rank={_rank}," +
                    $" Increase={rankIncrease}," +
                    $" Width={valueWidth}";

                return false;
            }

            if (!TryReadSigned(
                    ref reader,
                    valueWidth,
                    out int delta,
                    out error))
            {
                return false;
            }

            _previousValue =
                unchecked(
                    _previousValue +
                    delta);

            if (rankIncrease > 0)
            {
                _rank =
                    valueWidth;
            }
            else
            {
                /*
                 * 純正コードは、実際の差分に必要なrankが
                 * 現在rankより小さい場合に1段階だけ下げる。
                 */
                int actualRank =
                    GetSignedRank(delta);

                if (actualRank < _rank)
                {
                    _rank--;
                }
            }

            value =
                _previousValue;

            return true;
        }

        private static bool TryReadWidth(
            ref GL1BitReader reader,
            out int width,
            out string error)
        {
            width = 0;
            error = string.Empty;

            if (!reader.TryReadBits(
                    5,
                    out uint widthBits))
            {
                error =
                    "圧縮値幅の5bitが不足しています。";

                return false;
            }

            width =
                (int)widthBits;

            if (width > 31)
            {
                error =
                    $"圧縮値幅が不正です。" +
                    $" Width={width}";

                return false;
            }

            return true;
        }

        private static bool TryReadSigned(
            ref GL1BitReader reader,
            int width,
            out int value,
            out string error)
        {
            value = 0;
            error = string.Empty;

            if (width == 0)
            {
                return true;
            }

            if (width < 0 || width > 31)
            {
                error =
                    $"符号付き値幅が不正です。" +
                    $" Width={width}";

                return false;
            }

            if (!reader.TryReadBits(
                    width,
                    out uint bits))
            {
                error =
                    $"圧縮差分が不足しています。" +
                    $" Width={width}," +
                    $" Remaining={reader.RemainingBits}";

                return false;
            }

            /*
             * 純正コード：
             *
             * bits -
             * ((1 << width) & (bits << 1))
             *
             * 最上位bitが1なら2^widthを減算する。
             */
            uint signBit =
                1u << (width - 1);

            if ((bits & signBit) != 0)
            {
                value =
                    unchecked(
                        (int)(
                            bits -
                            (1u << width)));
            }
            else
            {
                value =
                    (int)bits;
            }

            return true;
        }

        private static int GetSignedRank(
            int value)
        {
            if (value == 0)
            {
                return 0;
            }

            if (value > 0)
            {
                /*
                 * 正数は符号bitが必要なので、
                 * 通常のbit長+1。
                 */
                return
                    33 -
                    BitOperations.LeadingZeroCount(
                        (uint)value);
            }

            /*
             * 負数は反転値のbit長+1。
             *
             * -1 → 1bit
             * -2 → 2bit
             * -3 → 3bit
             */
            return
                33 -
                BitOperations.LeadingZeroCount(
                    (uint)~value);
        }
    }
}