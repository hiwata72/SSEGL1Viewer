using System.Collections.Concurrent;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        private const int ScanSeconds = 10;

        public async Task<List<BluetoothDeviceInfo>> ScanAsync()
        {
            var foundDevices =
                new ConcurrentDictionary<ulong, BluetoothDeviceInfo>();

            var watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            void ReceivedHandler(
                BluetoothLEAdvertisementWatcher sender,
                BluetoothLEAdvertisementReceivedEventArgs args)
            {
                try
                {
                    BluetoothDeviceInfo receivedDevice =
                        CreateDeviceInfo(args);

                    foundDevices.AddOrUpdate(
                        args.BluetoothAddress,
                        receivedDevice,
                        (_, existingDevice) =>
                        {
                            existingDevice.MergeFrom(receivedDevice);
                            return existingDevice;
                        });
                }
                catch
                {
                    // 一部の広告データを解析できなくても、
                    // スキャン全体は継続する
                }
            }

            watcher.Received += ReceivedHandler;

            try
            {
                watcher.Start();

                await Task.Delay(
                    TimeSpan.FromSeconds(ScanSeconds));
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

        private static BluetoothDeviceInfo CreateDeviceInfo(
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            BluetoothLEAdvertisement advertisement =
                args.Advertisement;

            var device = new BluetoothDeviceInfo
            {
                Address =
                    FormatBluetoothAddress(args.BluetoothAddress),

                BluetoothAddress =
                    args.BluetoothAddress
            };

            device.UpdateName(advertisement.LocalName);
            device.UpdateRssi(args.RawSignalStrengthInDBm);

            foreach (Guid serviceUuid in advertisement.ServiceUuids)
            {
                device.AddServiceUuid(serviceUuid);
            }

            foreach (
                BluetoothLEManufacturerData manufacturer
                in advertisement.ManufacturerData)
            {
                device.AddManufacturerData(
                    FormatManufacturerData(manufacturer));
            }

            return device;
        }

        private static string FormatManufacturerData(
            BluetoothLEManufacturerData manufacturer)
        {
            string companyId =
                manufacturer.CompanyId.ToString("X4");

            IBuffer buffer = manufacturer.Data;

            if (buffer.Length == 0)
            {
                return $"CompanyId={companyId}";
            }

            byte[] bytes = new byte[(int)buffer.Length];

            using (DataReader reader = DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(bytes);
            }

            string dataText = BitConverter
                .ToString(bytes)
                .Replace("-", " ");

            return $"CompanyId={companyId} Data={dataText}";
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