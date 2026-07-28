using System.Collections.Concurrent;
using Windows.Devices.Bluetooth.Advertisement;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        private const int ScanSeconds = 8;

        public async Task<List<BluetoothDeviceInfo>> ScanAsync()
        {
            var foundDevices =
                new ConcurrentDictionary<ulong, BluetoothDeviceInfo>();

            var watcher = new BluetoothLEAdvertisementWatcher
            {
                // 機器名を含むスキャン応答も受信しやすくする
                ScanningMode = BluetoothLEScanningMode.Active
            };

            void ReceivedHandler(
                BluetoothLEAdvertisementWatcher sender,
                BluetoothLEAdvertisementReceivedEventArgs args)
            {
                ulong bluetoothAddress = args.BluetoothAddress;

                string name = args.Advertisement.LocalName;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "(名前なし)";
                }

                var device = new BluetoothDeviceInfo
                {
                    Name = name,
                    Address = FormatBluetoothAddress(bluetoothAddress),
                    BluetoothAddress = bluetoothAddress,
                    Rssi = args.RawSignalStrengthInDBm
                };

                foundDevices.AddOrUpdate(
                    bluetoothAddress,
                    device,
                    (_, existing) =>
                    {
                        // 後から機器名を取得できた場合は更新する
                        if (existing.Name == "(名前なし)" &&
                            device.Name != "(名前なし)")
                        {
                            existing.Name = device.Name;
                        }

                        existing.Rssi = device.Rssi;

                        return existing;
                    });
            }

            watcher.Received += ReceivedHandler;

            try
            {
                watcher.Start();

                await Task.Delay(TimeSpan.FromSeconds(ScanSeconds));
            }
            finally
            {
                watcher.Stop();
                watcher.Received -= ReceivedHandler;
            }

            return foundDevices.Values
                .OrderByDescending(device => device.Rssi)
                .ThenBy(device => device.Name)
                .ToList();
        }

        private static string FormatBluetoothAddress(ulong address)
        {
            return string.Join(
                ":",
                Enumerable.Range(0, 6)
                    .Reverse()
                    .Select(index =>
                        ((address >> (index * 8)) & 0xFF)
                        .ToString("X2")));
        }
    }
}