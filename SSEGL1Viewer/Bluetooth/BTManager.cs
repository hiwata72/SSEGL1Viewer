using System.Collections.Concurrent;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        private const int ScanSeconds = 20;

        public async Task<List<BluetoothDeviceInfo>> ScanAsync()
        {
            var foundDevices =
                new ConcurrentDictionary<string, BluetoothDeviceInfo>();

            // false = 未ペアリングのBluetooth Classic機器を検索
            string selector =
                BluetoothDevice.GetDeviceSelectorFromPairingState(false);

            DeviceWatcher watcher =
                DeviceInformation.CreateWatcher(selector);

            var scanCompleted =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            void AddedHandler(
                DeviceWatcher sender,
                DeviceInformation device)
            {
                try
                {
                    AddOrUpdateDevice(foundDevices, device);
                }
                catch
                {
                    // 個別機器の情報取得に失敗しても検索は継続する
                }
            }

            void UpdatedHandler(
                DeviceWatcher sender,
                DeviceInformationUpdate update)
            {
                if (!foundDevices.TryGetValue(
                        update.Id,
                        out BluetoothDeviceInfo? existing))
                {
                    return;
                }

                // 更新通知には名前が含まれない場合が多いため、
                // ここでは現在の情報を保持する
                existing.DeviceId = update.Id;
            }

            void EnumerationCompletedHandler(
                DeviceWatcher sender,
                object args)
            {
                scanCompleted.TrySetResult(true);
            }

            void StoppedHandler(
                DeviceWatcher sender,
                object args)
            {
                scanCompleted.TrySetResult(true);
            }

            watcher.Added += AddedHandler;
            watcher.Updated += UpdatedHandler;
            watcher.EnumerationCompleted += EnumerationCompletedHandler;
            watcher.Stopped += StoppedHandler;

            try
            {
                watcher.Start();

                Task timeoutTask =
                    Task.Delay(TimeSpan.FromSeconds(ScanSeconds));

                await Task.WhenAny(
                    scanCompleted.Task,
                    timeoutTask);
            }
            finally
            {
                if (watcher.Status == DeviceWatcherStatus.Started ||
                    watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    watcher.Stop();
                }

                watcher.Added -= AddedHandler;
                watcher.Updated -= UpdatedHandler;
                watcher.EnumerationCompleted -=
                    EnumerationCompletedHandler;
                watcher.Stopped -= StoppedHandler;
            }

            return foundDevices.Values
                .OrderByDescending(device =>
                    device.Name.StartsWith(
                        "SSE-GL1",
                        StringComparison.OrdinalIgnoreCase))
                .ThenBy(device => device.Name)
                .ToList();
        }

        public async Task<string> TestDirectAccessAsync()
        {
            const ulong sseGl1Address = 0x143FA6362161;

            using BluetoothDevice? device =
                await BluetoothDevice.FromBluetoothAddressAsync(
                    sseGl1Address);

            if (device is null)
            {
                return
                    "SSE-GL1をBluetoothアドレスから取得できませんでした。";
            }

            return
                $"取得成功: {device.Name}\r\n" +
                $"Address: {FormatBluetoothAddress(device.BluetoothAddress)}\r\n" +
                $"ConnectionStatus: {device.ConnectionStatus}";
        }


        public async Task<string> TestRfcommServicesAsync()
        {
            const ulong sseGl1Address = 0x143FA6362161;

            using BluetoothDevice? device =
                await BluetoothDevice.FromBluetoothAddressAsync(
                    sseGl1Address);

            if (device is null)
            {
                return "SSE-GL1を取得できませんでした。";
            }

            RfcommDeviceServicesResult result =
                await device.GetRfcommServicesAsync(
                    BluetoothCacheMode.Uncached);

            if (result.Error != BluetoothError.Success)
            {
                return
                    $"RFCOMMサービス取得失敗\r\n" +
                    $"Error: {result.Error}";
            }

            if (result.Services.Count == 0)
            {
                return
                    $"Device: {device.Name}\r\n" +
                    "RFCOMMサービスは見つかりませんでした。";
            }

            var lines = new List<string>
            {
                $"Device: {device.Name}",
                $"RFCOMMサービス数: {result.Services.Count}",
                ""
            };

            int index = 1;

            foreach (RfcommDeviceService service in result.Services)
            {
                lines.Add($"Service {index}");
                lines.Add($"  Name: {service.ConnectionServiceName}");
                lines.Add($"  ServiceId: {service.ServiceId.Uuid}");
                lines.Add($"  HostName: {service.ConnectionHostName}");
                lines.Add("");

                index++;
            }

            return string.Join("\r\n", lines);
        }


        private static void AddOrUpdateDevice(
            ConcurrentDictionary<string, BluetoothDeviceInfo> devices,
            DeviceInformation device)
        {
            string name = string.IsNullOrWhiteSpace(device.Name)
                ? "(名前なし)"
                : device.Name;

            var deviceInfo = new BluetoothDeviceInfo
            {
                Name = name,
                DeviceId = device.Id,
                IsPaired = device.Pairing.IsPaired
            };

            devices.AddOrUpdate(
                device.Id,
                deviceInfo,
                (_, existing) =>
                {
                    if (existing.Name == "(名前なし)" &&
                        name != "(名前なし)")
                    {
                        existing.Name = name;
                    }

                    existing.IsPaired =
                        device.Pairing.IsPaired;

                    return existing;
                });
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