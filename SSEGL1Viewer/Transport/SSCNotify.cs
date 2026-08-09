using System;

namespace SSEGL1Viewer.Transport
{
    public sealed class SSCNotify
    {
        public byte Tid { get; set; }

        public byte Category { get; set; }

        public byte Command { get; set; }

        public byte Flags { get; set; }

        public uint SourceAddress { get; set; }

        public uint DestinationAddress { get; set; }

        /// <summary>
        /// SSCヘッダー内のAR（Argument）。
        /// </summary>
        public uint Argument { get; set; }

        public byte[] Payload { get; set; } =
            Array.Empty<byte>();

        /// <summary>
        /// センサーデータ種別。
        /// Argumentの下位6bit。
        /// </summary>
        public int DataId =>
            (int)(Argument & 0x3F);

        /// <summary>
        /// タイムスタンプが差分形式か。
        /// </summary>
        public bool DeltaTime =>
            (Argument & 0x40) != 0;

        /// <summary>
        /// センサーデータが圧縮形式か。
        /// </summary>
        public bool Compressed =>
            (Argument & 0x80) != 0;
    }
}