using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "";

        public string Id { get; set; } = "";

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? "(Unknown)" : Name;
        }

    }
}
