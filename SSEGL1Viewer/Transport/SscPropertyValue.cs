using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSEGL1Viewer.Transport
{
    public sealed class SscPropertyValue
    {
        public string Name { get; init; } = string.Empty;

        public byte[] Payload { get; init; } =
            Array.Empty<byte>();

        public bool IsAsciiText { get; init; }

        public string Text { get; init; } = string.Empty;

        public string Hex =>
            BitConverter.ToString(Payload)
                .Replace("-", " ");
    }
}