using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "";

        public string Address { get; set; } = "";

        public override string ToString()
        {
            return $"{Name}    {Address}";
        }
    }
}
