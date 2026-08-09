using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSEGL1Viewer.Bluetooth
{
    public sealed class SseDeviceInformation
    {
        public string Model { get; init; } = string.Empty;

        public string Version { get; init; } = string.Empty;

        public string Serial { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Id { get; init; } = string.Empty;

        public string Region { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string Temperature { get; init; } = string.Empty;

    }

}