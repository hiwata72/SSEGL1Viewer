using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        public async Task<List<BluetoothDeviceInfo>> ScanAsync()
        {
            await Task.Delay(1000);

            return new List<BluetoothDeviceInfo>()
            {
                new BluetoothDeviceInfo()
                {
                    Name="SSE-GL1 (Sample)",
                    Address="00:11:22:33:44:55"
                }
            };
        }

    }
}