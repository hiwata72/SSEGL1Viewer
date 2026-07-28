using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        // Windows BLE API が参照できるか確認するためのテスト
        public void TestWindowsBleApi()
        {
            string selector = BluetoothLEDevice.GetDeviceSelector();

            _ = selector;
            _ = typeof(DeviceInformation);
        }

        // 仮実装（後で本物のBLE検索に置き換える）
        public async Task<List<BluetoothDeviceInfo>> ScanAsync()
        {
            // WinRT API が参照できることを確認
            TestWindowsBleApi();

            await Task.Delay(500);

            return new List<BluetoothDeviceInfo>
            {
                new BluetoothDeviceInfo
                {
                    Name = "SSE-GL1 (Sample)",
                    Address = "00:11:22:33:44:55"
                }
            };
        }
    }
}