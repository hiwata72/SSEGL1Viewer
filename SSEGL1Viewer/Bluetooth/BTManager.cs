using SSEGL1Viewer.Bluetooth;
using SSEGL1Viewer.Transport;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading;

namespace SSEGL1Viewer.Bluetooth
{
    internal class BTManager
    {
        private const ulong SseGl1Address = 0x143FA6362161;

        private BluetoothDevice? _device;
        private RfcommDeviceService? _serialPortService;
        private StreamSocket? _socket;

        private DataReader? _reader;
        private CancellationTokenSource? _receiveCancellation;

        private Task? _receiveTask;

        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public event Action<string>? DataReceived;

        public event Action<GL1DecodedPacket>? MotionDecoded;

        private DataWriter? _writer;

        private const int ScanSeconds = 20;

        private readonly SSCTidManager _tidManager = new();

        private readonly ConcurrentDictionary<byte, TaskCompletionSource<SSCReply>> _pendingReplies = new();

        private readonly List<byte> _receiveBuffer = new();

        private const int MaxReceiveBufferLength = 1024 * 1024;

        private readonly GL1CompressionDecoder
                    _gl1CompressionDecoder = new();

        private volatile bool _isSensorStreaming;

        private volatile bool _isStopRequested;

        private int _notifyLogCounter;

        public event Action<string>? ConnectionStatusChanged;

        // SSE-GL1 実機90度回転試験による暫定値。
        // 後で仕様値または精密校正値へ置き換える。
        private const double GyroScaleDegPerSecondPerCount =
            0.0572;

        private void SetConnectionStatus(string status)
        {
            ConnectionStatusChanged?.Invoke(status);
        }
         
        public static byte[] BuildDeviceModelPayload()
        {
            return System.Text.Encoding.ASCII.GetBytes(
                "device.model\0");
        }

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

            // Uncachedでサービスが見つからなかった場合、
            // Windows側のキャッシュも確認する
            if (result.Error == BluetoothError.Success &&
                result.Services.Count == 0)
            {
                result =
                    await device.GetRfcommServicesAsync(
                        BluetoothCacheMode.Cached);
            }


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


        private static readonly Guid SscServiceUuid = Guid.Parse("188607d7-c970-4ed6-ae7e-26d2fe215927");

        private static readonly RfcommServiceId SscServiceId =
            RfcommServiceId.FromUuid(SscServiceUuid);

        public async Task<string> ConnectAsync()
        {
            Disconnect();

            _device =
                await BluetoothDevice.FromBluetoothAddressAsync(
                    SseGl1Address);

            Debug.WriteLine(
                $"[Connect] Device: " +
                $"{(_device is null ? "NULL" : _device.Name)}");

            if (_device is null)
            {
                return "SSE-GL1を取得できませんでした。";
            }

            /*
             * まずWindows側のキャッシュから
             * SSCサービスを取得する。
             */
            Debug.WriteLine(
                "[Connect] Cached取得開始");

            RfcommDeviceServicesResult servicesResult =
                await _device.GetRfcommServicesForIdAsync(
                    SscServiceId,
                    BluetoothCacheMode.Cached);

            Debug.WriteLine(
                $"[Connect] Cached取得完了: " +
                $"Error={servicesResult.Error}, " +
                $"Count={servicesResult.Services.Count}");

            if (servicesResult.Error !=
                BluetoothError.Success)
            {
                Disconnect();

                return
                    "SSCサービスの取得に失敗しました。\r\n" +
                    $"UUID: {SscServiceUuid}\r\n" +
                    $"Error: {servicesResult.Error}";
            }

            if (servicesResult.Services.Count == 0)
            {
                Disconnect();

                return
                    "SSCサービスが見つかりませんでした。\r\n" +
                    $"UUID: {SscServiceUuid}";
            }

            /*
             * 接続処理中はローカル変数で
             * RfcommDeviceServiceを保持する。
             */
            RfcommDeviceService serialPortService =
                servicesResult.Services[0];

            _serialPortService =
                serialPortService;

            Debug.WriteLine(
                $"[Connect] Service UUID: " +
                $"{serialPortService.ServiceId.Uuid}");

            DeviceAccessStatus accessStatus =
                await serialPortService.RequestAccessAsync();

            Debug.WriteLine(
                $"[Connect] AccessStatus: " +
                $"{accessStatus}");

            if (accessStatus !=
                DeviceAccessStatus.Allowed)
            {
                Disconnect();

                return
                    "SSCサービスへのアクセスが" +
                    "許可されませんでした。\r\n" +
                    $"AccessStatus: {accessStatus}";
            }

            SocketProtectionLevel[] protectionLevels =
             {
                SocketProtectionLevel
                    .BluetoothEncryptionAllowNullAuthentication,

                SocketProtectionLevel
                    .BluetoothEncryptionWithAuthentication,

                SocketProtectionLevel
                    .PlainSocket
            };

            var errors =
                new List<string>();

            foreach (
                SocketProtectionLevel protectionLevel
                in protectionLevels)
            {
                _socket?.Dispose();

                _socket =
                    new StreamSocket();

                try
                {
                    Debug.WriteLine(
                        $"[Connect] Socket開始: " +
                        $"{protectionLevel}");

                    Debug.WriteLine(
                        $"[Connect] Socket開始: {protectionLevel}");

                    Task connectTask =
                        _socket.ConnectAsync(
                            serialPortService.ConnectionHostName,
                            serialPortService.ConnectionServiceName,
                            protectionLevel)
                        .AsTask();

                    Task completedTask =
                        await Task.WhenAny(
                            connectTask,
                            Task.Delay(
                                TimeSpan.FromSeconds(10)));

                    if (completedTask != connectTask)
                    {
                        Debug.WriteLine(
                            $"[Connect] Socketタイムアウト: " +
                            $"{protectionLevel}");

                        _socket.Dispose();
                        _socket = null;

                        throw new TimeoutException(
                            $"RFCOMM接続が10秒でタイムアウトしました。" +
                            $" Protection={protectionLevel}");
                    }

                    await connectTask;

                    Debug.WriteLine(
                        $"[Connect] Socket成功: {protectionLevel}");
                    Debug.WriteLine(
                        $"[Connect] Socket成功: " +
                        $"{protectionLevel}");

                    _writer?.Dispose();

                    _writer =
                        new DataWriter(
                            _socket.OutputStream);

                    StartReceive();

                    DataReceived?.Invoke(
                        $"{DateTime.Now:HH:mm:ss.fff} " +
                        $"[接続完了]\r\n" +
                        $"Socket     : OK\r\n" +
                        $"Writer     : OK\r\n" +
                        $"Service    : " +
                        $"{serialPortService.ServiceId.Uuid}\r\n" +
                        $"Protection : " +
                        $"{protectionLevel}");

                    SetConnectionStatus(
                        "接続済み");

                    return
                        $"接続成功: {_device.Name}\r\n" +
                        $"Address: " +
                        $"{FormatBluetoothAddress(
                            _device.BluetoothAddress)}\r\n" +
                        $"Service: " +
                        $"{serialPortService.ServiceId.Uuid}\r\n" +
                        $"Protection: " +
                        $"{protectionLevel}\r\n" +
                        $"ConnectionStatus: " +
                        $"{_device.ConnectionStatus}";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"[Connect] Socket失敗: " +
                        $"{protectionLevel}, " +
                        $"{ex.GetType().Name}, " +
                        $"0x{ex.HResult:X8}, " +
                        $"{ex.Message}");

                    errors.Add(
                        $"{protectionLevel}: " +
                        $"{ex.GetType().Name} " +
                        $"0x{ex.HResult:X8} " +
                        $"{ex.Message}");

                    _writer?.Dispose();
                    _writer = null;

                    _socket?.Dispose();
                    _socket = null;
                }
            }

            Disconnect();

            return
                "SSCサービスへの接続に失敗しました。" +
                "\r\n\r\n" +
                string.Join(
                    "\r\n",
                    errors);
        }

        public async Task<string> DumpRfcommServicesAsync()
        {
            BluetoothDevice? device =
                await BluetoothDevice.FromBluetoothAddressAsync(
                    SseGl1Address);

            if (device is null)
            {
                return
                    "SSE-GL1を取得できませんでした。";
            }

            Debug.WriteLine(
                $"[RFCOMM] Device: {device.Name}");

            Debug.WriteLine(
                "[RFCOMM] Uncached全サービス取得開始");

            RfcommDeviceServicesResult result =
                await device.GetRfcommServicesAsync(
                    BluetoothCacheMode.Uncached);

            Debug.WriteLine(
                $"[RFCOMM] Uncached取得完了: " +
                $"Error={result.Error}, " +
                $"Count={result.Services.Count}");

            // Uncachedで取得できなければCachedも確認する
            if (result.Error != BluetoothError.Success ||
                result.Services.Count == 0)
            {
                Debug.WriteLine(
                    "[RFCOMM] Cached全サービス取得開始");

                result =
                    await device.GetRfcommServicesAsync(
                        BluetoothCacheMode.Cached);

                Debug.WriteLine(
                    $"[RFCOMM] Cached取得完了: " +
                    $"Error={result.Error}, " +
                    $"Count={result.Services.Count}");
            }

            if (result.Error != BluetoothError.Success)
            {
                return
                    "RFCOMMサービス取得失敗\r\n" +
                    $"Error: {result.Error}";
            }

            if (result.Services.Count == 0)
            {
                return
                    $"Device: {device.Name}\r\n" +
                    "RFCOMMサービスは見つかりませんでした。";
            }

            var lines =
                new List<string>
                {
            $"Device: {device.Name}",
            $"RFCOMMサービス数: {result.Services.Count}",
            ""
                };

            for (int index = 0;
                 index < result.Services.Count;
                 index++)
            {
                RfcommDeviceService service =
                    result.Services[index];

                lines.Add(
                    $"Service[{index}]");

                lines.Add(
                    $"UUID: " +
                    $"{service.ServiceId.Uuid}");

                lines.Add(
                    $"Host: " +
                    $"{service.ConnectionHostName}");

                lines.Add(
                    $"ServiceName: " +
                    $"{service.ConnectionServiceName}");

                lines.Add(
                    $"MaxProtectionLevel: " +
                    $"{service.MaxProtectionLevel}");

                lines.Add("");

                Debug.WriteLine(
                    $"[RFCOMM] Service[{index}] " +
                    $"UUID={service.ServiceId.Uuid}, " +
                    $"Host={service.ConnectionHostName}, " +
                    $"ServiceName={service.ConnectionServiceName}, " +
                    $"MaxProtection={service.MaxProtectionLevel}");
            }

            return
                string.Join(
                    "\r\n",
                    lines);
        }

        public async Task<string> TestRfcommConnectAsync()
        {
            BluetoothDevice? device =
                await BluetoothDevice.FromBluetoothAddressAsync(
                    SseGl1Address);

            if (device is null)
            {
                return "SSE-GL1を取得できませんでした。";
            }

            RfcommDeviceServicesResult result =
                await device.GetRfcommServicesAsync(
                    BluetoothCacheMode.Uncached);

            if (result.Error != BluetoothError.Success ||
                result.Services.Count == 0)
            {
                result =
                    await device.GetRfcommServicesAsync(
                        BluetoothCacheMode.Cached);
            }

            if (result.Error != BluetoothError.Success)
            {
                return
                    "RFCOMMサービス取得失敗\r\n" +
                    $"Error: {result.Error}";
            }

            if (result.Services.Count == 0)
            {
                return "RFCOMMサービスが見つかりませんでした。";
            }

            var lines = new List<string>();

            for (int index = 0;
                 index < result.Services.Count;
                 index++)
            {
                RfcommDeviceService service =
                    result.Services[index];

                lines.Add(
                    $"Service[{index}]");
                lines.Add(
                    $"UUID: {service.ServiceId.Uuid}");

                try
                {
                    using StreamSocket socket =
                        new StreamSocket();

                    lines.Add("接続開始...");

                    await socket.ConnectAsync(
                        service.ConnectionHostName,
                        service.ConnectionServiceName,
                        SocketProtectionLevel
                            .BluetoothEncryptionWithAuthentication);

                    lines.Add("接続成功");

                    Debug.WriteLine(
                        $"[RFCOMM-CONNECT] " +
                        $"Service[{index}] 接続成功 " +
                        $"UUID={service.ServiceId.Uuid}");
                }
                catch (Exception ex)
                {
                    lines.Add(
                        $"接続失敗: " +
                        $"{ex.GetType().Name}");

                    lines.Add(
                        ex.Message);

                    Debug.WriteLine(
                        $"[RFCOMM-CONNECT] " +
                        $"Service[{index}] 接続失敗 " +
                        $"{ex}");
                }

                lines.Add("");
            }

            return string.Join(
                "\r\n",
                lines);
        }

        private void StartReceive()
        {
            if (_socket is null)
            {
                return;
            }

            _receiveBuffer.Clear();

            _receiveCancellation?.Cancel();
            _receiveCancellation?.Dispose();

            _receiveCancellation = new CancellationTokenSource();

            _reader?.Dispose();

            _reader = new DataReader(_socket.InputStream)
            {
                InputStreamOptions = InputStreamOptions.Partial
            };

            _receiveTask = ReceiveLoopAsync(
                _receiveCancellation.Token);
        }

        public async Task<byte> SendSscRequestAsync(byte command, byte[] payload) 
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_writer is null)
            {
                throw new InvalidOperationException(
                    "Bluetooth接続されていません。");
            }

            byte tid = _tidManager.Acquire();

            try
            {
                byte[] frame = SSCTransport.BuildFrame(tid, command, payload, 0xF0);

                string frameHex =
                    BitConverter.ToString(frame).Replace("-", " ");

                DataReceived?.Invoke(
                    $"SSC Frame\r\n" +
                    $"TID   : {tid}\r\n" +
                    $"HEX   : {frameHex}");

                await SendHexAsync(frameHex);

                _ = WatchSscReplyTimeoutAsync(
                    tid,
                    TimeSpan.FromSeconds(3));

                return tid;
            }
            catch
            {
                _tidManager.Cancel(tid);
                throw;
            }
        }

        public void Disconnect()
        {
            _receiveBuffer.Clear();

            _receiveCancellation?.Cancel();

            _reader?.Dispose();
            _reader = null;

            _writer?.Dispose();
            _writer = null;

            _socket?.Dispose();
            _socket = null;

            _serialPortService?.Dispose();
            _serialPortService = null;

            _device?.Dispose();
            _device = null;

            _receiveCancellation?.Dispose();
            _receiveCancellation = null;

            _receiveTask = null;

            _tidManager.Clear();

            _gl1CompressionDecoder.Reset();

            SetConnectionStatus("未接続");
        }

        private bool TryExtractNextSscFrame(out byte[] frame)
        {
            frame = Array.Empty<byte>();

            while (true)
            {
                // 最小フレーム：
                // TID + Command + Flags + C9
                if (_receiveBuffer.Count < 4)
                {
                    return false;
                }

                bool foundHeader = false;
                bool waitingForRemainder = false;

                int frameStart = -1;
                int frameLength = 0;

                /*
                 * Payload内にも0xC9が含まれる可能性があるため、
                 * バッファ内の0xC9を先頭から順番に調べます。
                 */
                for (int delimiterIndex = 3;
                     delimiterIndex < _receiveBuffer.Count;
                     delimiterIndex++)
                {
                    if (_receiveBuffer[delimiterIndex] != 0xC9)
                    {
                        continue;
                    }

                    byte tidByte =
                        _receiveBuffer[delimiterIndex - 3];

                    byte tid =
                        (byte)(tidByte & 0x0F);

                    byte tidComplement =
                        (byte)(tidByte >> 4);

                    // ヘッダー終端候補かをTID補数で確認
                    if (tidComplement !=
                        (byte)(tid ^ 0x0F))
                    {
                        continue;
                    }

                    byte commandByte =
                        _receiveBuffer[delimiterIndex - 2];

                    byte flags =
                        _receiveBuffer[delimiterIndex - 1];

                    byte lengthMode =
                        (byte)(flags & 0x03);

                    bool hasSourceAddress =
                        (flags & 0x20) != 0;

                    bool hasDestinationAddress =
                        (flags & 0x10) != 0;

                    int argumentLength =
                        (commandByte >> 6) switch
                        {
                            0 => 0,
                            1 => 1,
                            2 => 2,
                            3 => 4,
                            _ => 0
                        };

                    int payloadLengthFieldLength;

                    switch (lengthMode)
                    {
                        case 0:
                            payloadLengthFieldLength = 0;
                            break;

                        case 2:
                            payloadLengthFieldLength = 1;
                            break;

                        // LengthMode 1、3はまだ未対応。
                        default:
                            continue;
                    }

                    int sourceAddressLength =
                        hasSourceAddress ? 1 : 0;

                    int destinationAddressLength =
                        hasDestinationAddress ? 1 : 0;

                    /*
                     * C9より前にあるフィールド：
                     *
                     * Argument
                     * DA
                     * SA
                     * PayloadLength
                     * TID
                     * Command
                     * Flags
                     */
                    int variableHeaderLength =
                        argumentLength +
                        destinationAddressLength +
                        sourceAddressLength +
                        payloadLengthFieldLength;

                    int candidateFrameStart =
                        delimiterIndex -
                        3 -
                        variableHeaderLength;

                    if (candidateFrameStart < 0)
                    {
                        // このC9より前のデータが不足している。
                        continue;
                    }

                    /*
                     * C9側から逆向きに、Payload長フィールドの位置を求める。
                     */
                    int cursor =
                        delimiterIndex - 4;

                    int payloadLength;

                    switch (lengthMode)
                    {
                        case 0:
                            payloadLength = 0;
                            break;

                        case 2:
                            if (cursor < candidateFrameStart)
                            {
                                continue;
                            }

                            // encodedLength = payloadLength - 1
                            payloadLength =
                                _receiveBuffer[cursor] + 1;

                            cursor--;
                            break;

                        default:
                            continue;
                    }

                    // SA
                    if (hasSourceAddress)
                    {
                        if (cursor < candidateFrameStart)
                        {
                            continue;
                        }

                        cursor--;
                    }

                    // DA
                    if (hasDestinationAddress)
                    {
                        if (cursor < candidateFrameStart)
                        {
                            continue;
                        }

                        cursor--;
                    }

                    // Argument
                    cursor -= argumentLength;

                    /*
                     * 全可変フィールドを読み終えた位置が、
                     * candidateFrameStartの直前であることを確認します。
                     */
                    if (cursor != candidateFrameStart - 1)
                    {
                        continue;
                    }

                    int paddingLength =
                        payloadLength > 0
                            ? (-payloadLength) & 3
                            : 0;

                    // 受信フレームにはPaddingなし。
                    // Payloadありの場合はCRC 2バイトのみ。
                    int trailerLength =
                        payloadLength > 0
                            ? 2
                            : 0;

                    int candidateFrameLength =
                        (delimiterIndex -
                         candidateFrameStart +
                         1) +
                        payloadLength +
                        trailerLength;

                    int candidateFrameEnd =
                        candidateFrameStart +
                        candidateFrameLength;

                    foundHeader = true;

                    if (_receiveBuffer.Count <
                        candidateFrameEnd)
                    {
                        /*
                         * 正常らしいヘッダーは見つかったが、
                         * PayloadまたはCRCがまだ届いていない。
                         */
                        if (candidateFrameStart > 0)
                        {
                            _receiveBuffer.RemoveRange(
                                0,
                                candidateFrameStart);
                        }

                        waitingForRemainder = true;
                        break;
                    }

                    frameStart =
                        candidateFrameStart;

                    frameLength =
                        candidateFrameLength;

                    break;
                }

                if (waitingForRemainder)
                {
                    return false;
                }

                if (frameStart >= 0)
                {
                    /*
                     * フレーム前に残っている途中データやゴミを破棄。
                     */
                    if (frameStart > 0)
                    {
                        _receiveBuffer.RemoveRange(
                            0,
                            frameStart);
                    }

                    frame =
                        _receiveBuffer
                            .GetRange(
                                0,
                                frameLength)
                            .ToArray();

                    _receiveBuffer.RemoveRange(
                        0,
                        frameLength);

                    return true;
                }

                if (foundHeader)
                {
                    // ヘッダー候補はあったが確定できなかった。
                    return false;
                }

                /*
                 * 有効なヘッダー候補がない場合。
                 *
                 * 最後の数バイトは次回受信のヘッダー前半かもしれないため、
                 * 全消去せず末尾6バイトを残します。
                 */
                const int preserveLength = 6;

                if (_receiveBuffer.Count >
                    preserveLength)
                {
                    int removeLength =
                        _receiveBuffer.Count -
                        preserveLength;

                    _receiveBuffer.RemoveRange(
                        0,
                        removeLength);
                }

                if (_receiveBuffer.Count >
                    MaxReceiveBufferLength)
                {
                    _receiveBuffer.Clear();

                    DataReceived?.Invoke(
                        "SSC受信バッファが上限を超えたため、" +
                        "クリアしました。");
                }

                return false;
            }
        }

        private int FindSscHeaderDelimiter()
        {
            for (int index = 3;
                 index < _receiveBuffer.Count;
                 index++)
            {
                if (_receiveBuffer[index] != 0xC9)
                {
                    continue;
                }

                byte tidByte =
                    _receiveBuffer[index - 3];

                byte tid =
                    (byte)(tidByte & 0x0F);

                byte complement =
                    (byte)(tidByte >> 4);

                // ヘッダー終端候補かをTID補数で確認する
                if (complement ==
                    (byte)(tid ^ 0x0F))
                {
                    return index;
                }
            }

            return -1;
        }
        private void ProcessSscFrame(byte[] frame)
        {
            string frameHex =
                BitConverter.ToString(frame)
                    .Replace("-", " ");

            /*
             * 1. Notify解析
             */
            if (SSCNotifyParser.TryParse(
                    frame,
                    out SSCNotify? notify,
                    out string notifyParseError))
            {
                // 停止要求中は、受信キューに残っているNotifyを
                // ログ表示・圧縮展開せず捨てる。
                if (_isStopRequested)
                {
                    return;
                }

                // ストリーミング停止後のNotifyは処理しない。
                if (!_isSensorStreaming)
                {
                    return;
                }

                // 50フレームに1回だけ詳細ログを出す。
                bool writeNotifyLog =
                    Interlocked.Increment(
                        ref _notifyLogCounter) % 50 == 1;

                if (writeNotifyLog)
                {
                    string payloadHex =
                        notify!.Payload.Length == 0
                            ? "(なし)"
                            : BitConverter
                                .ToString(notify.Payload)
                                .Replace("-", " ");

                    DataReceived?.Invoke(
                        $"SSC Notify\r\n" +
                        $"Frame bytes: {frame.Length}\r\n" +
                        $"Category   : {notify.Category}\r\n" +
                        $"TID        : {notify.Tid}\r\n" +
                        $"Command    : {notify.Command}\r\n" +
                        $"Flags      : 0x{notify.Flags:X2}\r\n" +
                        $"SA         : 0x{notify.SourceAddress:X8}\r\n" +
                        $"DA         : 0x{notify.DestinationAddress:X8}\r\n" +
                        $"Argument   : 0x{notify.Argument:X8}\r\n" +
                        $"Data ID    : {notify.DataId}\r\n" +
                        $"Delta Time : {notify.DeltaTime}\r\n" +
                        $"Compressed : {notify.Compressed}\r\n" +
                        $"Payload HEX: {payloadHex}");
                }

                if (_gl1CompressionDecoder.TryDecode(
                        notify!,
                        out GL1DecodedPacket? decodedPacket,
                        out string decodeError))
                {
                    // フォーム側へのモーションイベントは毎回送る。
                    MotionDecoded?.Invoke(decodedPacket!);

                    // 長いテキストログだけ間引く。
                    if (writeNotifyLog)
                    {
                        DataReceived?.Invoke(
                            $"GL1 Decoded\r\n" +
                            $"Data ID     : {decodedPacket!.DataId}\r\n" +
                            $"Timestamp   : " +
                            $"{decodedPacket.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss.fffffff zzz}\r\n" +
                            $"Timestamp ns: " +
                            $"{decodedPacket.TimestampNanoseconds}\r\n" +
                            $"Compressed  : " +
                            $"{decodedPacket.IsCompressed}\r\n" +
                            $"Samples     : " +
                            $"{decodedPacket.Samples.Count}");

                        if (decodedPacket.Samples.Count > 0)
                        {
                            GL1DecodedSample first =
                                decodedPacket.Samples[0];

                            string secondaryText =
                                first.Secondary.HasValue
                                    ? FormatVector(
                                        first.Secondary.Value)
                                    : "(なし)";

                            DataReceived?.Invoke(
                                $"GL1 First Sample\r\n" +
                                $"Primary   : " +
                                $"{FormatVector(first.Primary)}\r\n" +
                                $"Secondary : " +
                                $"{secondaryText}");
                        }
                    }
                }
                else if (writeNotifyLog)
                {
                    DataReceived?.Invoke(
                        $"GL1 Decode Error : {decodeError}");
                }

                return;
            }

            /*
             * 2. Reply解析
             */
            if (SSCReplyParser.TryParse(
                    frame,
                    out SSCReply? parsedReply,
                    out string replyParseError))
            {
                string payloadText =
                    parsedReply!.Payload.Length == 0
                        ? "(なし)"
                        : Encoding.ASCII
                            .GetString(parsedReply.Payload)
                            .TrimEnd('\0');

                bool tidMatched = false;

                if (parsedReply.Category == 2 &&
                    _pendingReplies.TryRemove(
                        parsedReply.Tid,
                        out TaskCompletionSource<SSCReply>?
                            pendingReply))
                {
                    _tidManager.Complete(
                        parsedReply.Tid);

                    tidMatched =
                        pendingReply.TrySetResult(
                            parsedReply);
                }

                string payloadHex =
                    parsedReply.Payload.Length == 0
                        ? "(なし)"
                        : BitConverter
                            .ToString(parsedReply.Payload)
                            .Replace("-", " ");

                DataReceived?.Invoke(
                    $"SSC Reply\r\n" +
                    $"Category   : {parsedReply.Category}\r\n" +
                    $"TID        : {parsedReply.Tid}\r\n" +
                    $"TID待機    : " +
                    $"{(tidMatched ? "一致・解除" : "一致なし")}\r\n" +
                    $"Command    : {parsedReply.Command}\r\n" +
                    $"Flags      : 0x{parsedReply.Flags:X2}\r\n" +
                    $"SA         : 0x{parsedReply.SourceAddress:X8}\r\n" +
                    $"DA         : 0x{parsedReply.DestinationAddress:X8}\r\n" +
                    $"Payload HEX: {payloadHex}\r\n" +
                    $"Payload    : {payloadText}");

                return;
            }

            /*
             * 3. NotifyでもReplyでもない場合
             */
            DataReceived?.Invoke(
                $"SSC Frame Parse Error\r\n" +
                $"Frame  : {frameHex}\r\n" +
                $"Notify : {notifyParseError}\r\n" +
                $"Reply  : {replyParseError}");
        }



        private static string FormatVector(
            MotionVector3 value)
        {
            return
                $"X={value.X}, " +
                $"Y={value.Y}, " +
                $"Z={value.Z}";
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            if (_reader is null)
            {
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    uint loadedLength =
                        await _reader.LoadAsync(1024);

                    if (loadedLength == 0)
                    {
                        DataReceived?.Invoke(
                            "[接続が切断されました。]");

                        break;
                    }

                    byte[] buffer = new byte[loadedLength];

                    _reader.ReadBytes(buffer);

                    string hexText =
                        BitConverter.ToString(buffer)
                            .Replace("-", " ");

                    string asciiText =
                        new string(
                            buffer.Select(
                                value =>
                                    value >= 0x20 &&
                                    value <= 0x7E
                                        ? (char)value
                                        : '.')
                            .ToArray());

                    string message =
                        $"{DateTime.Now:HH:mm:ss.fff} " +
                        $"[{buffer.Length} bytes]\r\n" +
                        $"HEX   : {hexText}\r\n" +
                        $"ASCII : {asciiText}";

                    // センサーストリーミング中は生HEXログを表示しない。
                    // 受信・フレーム解析自体は継続する。
                    if (!_isStopRequested &&
                        !_isSensorStreaming)
                    {
                        DataReceived?.Invoke(message);
                    }

                    _receiveBuffer.AddRange(buffer);

                    if (_receiveBuffer.Count >
                        MaxReceiveBufferLength)
                    {
                        _receiveBuffer.Clear();

                        DataReceived?.Invoke(
                            "SSC受信バッファが上限を超えたため、" +
                            "クリアしました。");

                        continue;
                    }

                    while (TryExtractNextSscFrame(
                               out byte[] sscFrame))
                    {
                        ProcessSscFrame(sscFrame);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // 切断処理による正常終了
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    DataReceived?.Invoke(
                        "[受信エラー]\r\n" +
                        $"{ex.GetType().Name}\r\n" +
                        $"0x{ex.HResult:X8}\r\n" +
                        ex.Message);
                }
            }
        }

        public async Task SendHexAsync(string hexText)
        {
            if (_socket is null)
            {
                throw new InvalidOperationException(
                    "送信できません。\r\n_socket が null です。");
            }

            if (_writer is null)
            {
                throw new InvalidOperationException(
                    "送信できません。\r\n_writer が null です。");
            }

            byte[] data = ParseHexString(hexText);

            if (data.Length == 0)
            {
                throw new ArgumentException(
                    "送信データが空です。",
                    nameof(hexText));
            }

            await _sendLock.WaitAsync();

            try
            {
                _writer.WriteBytes(data);

                uint storedLength = await _writer.StoreAsync();

                await _writer.FlushAsync();

                string formattedHex =
                    BitConverter.ToString(data)
                        .Replace("-", " ");

                DataReceived?.Invoke(
                    $"{DateTime.Now:HH:mm:ss.fff} " +
                    $"[送信 {storedLength} bytes]\r\n" +
                    $"HEX   : {formattedHex}");
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private static byte[] ParseHexString(string hexText)
        {
            if (string.IsNullOrWhiteSpace(hexText))
            {
                return Array.Empty<byte>();
            }

            string normalized =
                hexText
                    .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace(",", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "");

            if (normalized.Length % 2 != 0)
            {
                throw new FormatException(
                    "HEX文字列は2桁単位で入力してください。");
            }

            byte[] result = new byte[normalized.Length / 2];

            for (int index = 0; index < result.Length; index++)
            {
                string byteText =
                    normalized.Substring(index * 2, 2);

                if (!byte.TryParse(
                        byteText,
                        System.Globalization.NumberStyles.HexNumber,
                        null,
                        out result[index]))
                {
                    throw new FormatException(
                        $"HEXとして解釈できません: {byteText}");
                }
            }

            return result;
        }
        private async Task WatchSscReplyTimeoutAsync(byte tid, TimeSpan timeout)
        {
            await Task.Delay(timeout);

            if (_tidManager.Complete(tid))
            {
                DataReceived?.Invoke(
                    $"SSC Reply Timeout\r\n" +
                    $"TID : {tid}\r\n" +
                    $"待機解除しました。");
            }
        }


        public async Task<SSCReply> SendSscRequestAndWaitAsync(byte command, byte[] payload, TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_writer is null)
            {
                throw new InvalidOperationException(
                    "Bluetooth接続されていません。");
            }

            byte tid = _tidManager.Acquire();

            var completionSource =
                new TaskCompletionSource<SSCReply>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pendingReplies.TryAdd(tid, completionSource))
            {
                _tidManager.Cancel(tid);

                throw new InvalidOperationException(
                    $"TID {tid} のreply待機登録に失敗しました。");
            }

            try
            {
                byte[] frame = SSCTransport.BuildFrame(
                    tid,
                    command,
                    payload,
                    0xF0);

                string frameHex =
                    BitConverter.ToString(frame).Replace("-", " ");

                DataReceived?.Invoke(
                    $"SSC Frame\r\n" +
                    $"TID   : {tid}\r\n" +
                    $"HEX   : {frameHex}");

                await SendHexAsync(frameHex);

                TimeSpan replyTimeout =
                    timeout ?? TimeSpan.FromSeconds(3);

                using var cancellationSource =
                    new CancellationTokenSource(replyTimeout);

                using CancellationTokenRegistration registration =
                    cancellationSource.Token.Register(
                        () => completionSource.TrySetException(
                            new TimeoutException(
                                $"SSC Reply Timeout: TID {tid}")));

                return await completionSource.Task;
            }
            finally
            {
                _pendingReplies.TryRemove(tid, out _);
                _tidManager.Cancel(tid);
            }
        }

        public async Task<SSCReply> SendSscRequestAndWaitAsync(
            byte command,
            uint argument,
            byte[] payload,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_writer is null)
            {
                throw new InvalidOperationException(
                    "Bluetooth接続されていません。");
            }

            byte tid = _tidManager.Acquire();

            var completionSource =
                new TaskCompletionSource<SSCReply>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pendingReplies.TryAdd(tid, completionSource))
            {
                _tidManager.Cancel(tid);

                throw new InvalidOperationException(
                    $"TID {tid} のreply待機登録に失敗しました。");
            }

            try
            {
                byte[] frame =
                    SSCTransport.BuildFrameWithArgument(
                    tid: tid,
                    command: command,
                    argument: argument,
                    argumentLength: 1,
                    payload: payload,
                    destinationAddress: 0x80);

                string frameHex =
                    BitConverter.ToString(frame)
                        .Replace("-", " ");

                DataReceived?.Invoke(
                    $"SSC Frame\r\n" +
                    $"TID      : {tid}\r\n" +
                    $"Argument : 0x{argument:X8}\r\n" +
                    $"HEX      : {frameHex}");

                await SendHexAsync(frameHex);

                TimeSpan replyTimeout =
                    timeout ?? TimeSpan.FromSeconds(3);

                using var cancellationSource =
                    new CancellationTokenSource(replyTimeout);

                using CancellationTokenRegistration registration =
                    cancellationSource.Token.Register(
                        () => completionSource.TrySetException(
                            new TimeoutException(
                                $"SSC Reply Timeout: TID {tid}")));

                return await completionSource.Task;
            }
            finally
            {
                _pendingReplies.TryRemove(tid, out _);
                _tidManager.Cancel(tid);
            }
        }

        /// <summary>
        /// SSE-GL1の通常ゴルフモードを開始します。
        /// </summary>
        public async Task<SSCReply> StartGolfAsync(
            TimeSpan? timeout = null)
        {
            // SSC::GL1::start("golf") の解析結果。
            // Argument 0x03 = golf
            // Payload       = 02 01 0C
            byte[] payload = {0x02, 0x01, 0x0C};

            DataReceived?.Invoke(
                "SSC Sensor Start\r\n" +
                "Mode       : golf\r\n" +
                "Command    : 0x01\r\n" +
                "Argument   : 0x03\r\n" +
                "Payload    : 02 01 0C");

            SSCReply reply =
                await SendSscRequestAndWaitAsync(
                    command: 0x01,
                    argument: 0x03,
                    payload: payload,
                    timeout: timeout);

            if (reply.Command != 0x00)
            {
                string payloadHex =
                    reply.Payload.Length == 0
                        ? "(なし)"
                        : BitConverter.ToString(reply.Payload)
                            .Replace("-", " ");

                throw new InvalidOperationException(
                    "センサー開始要求に失敗しました。\r\n" +
                    $"Reply Command : 0x{reply.Command:X2}\r\n" +
                    $"Payload       : {payloadHex}");
            }

            _notifyLogCounter = 0;
            _isSensorStreaming = true;


            DataReceived?.Invoke(
                "SSC Sensor Start Reply\r\n" +
                "Result : 成功");

            SetConnectionStatus("Notify受信中");

            return reply;
        }

        /// <summary>
        /// SSE-GL1のセンサーストリームを停止します。
        /// </summary>
        public async Task<SSCReply> StopSensorAsync(
            TimeSpan? timeout = null)
        {
            DataReceived?.Invoke(
                "SSC Sensor Stop\r\n" +
                "Command : 0x02\r\n" +
                "Payload : (なし)");

            _isStopRequested = true;

            try
            {
                SSCReply reply =
                    await SendSscRequestAndWaitAsync(
                        command: 0x02,
                        payload: Array.Empty<byte>(),
                        destinationAddress: 0x80,
                        timeout: timeout ?? TimeSpan.FromSeconds(5));

                if (reply.Command != 0x00)
                {
                    throw new InvalidOperationException(
                        "センサー停止要求に失敗しました。\r\n" +
                        $"Reply Command : 0x{reply.Command:X2}");
                }

                _isSensorStreaming = false;
                _gl1CompressionDecoder.Reset();

                DataReceived?.Invoke(
                    "SSC Sensor Stop Reply\r\n" +
                    "Result : 成功");

                SetConnectionStatus("接続済み");

                return reply;
            }
            finally
            {
                _isStopRequested = false;
            }
        }

        public async Task<SSCReply> SendSscRequestAndWaitAsync(
            byte command,
            byte[] payload,
            byte destinationAddress,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_writer is null)
            {
                throw new InvalidOperationException(
                    "Bluetooth接続されていません。");
            }

            byte tid = _tidManager.Acquire();

            var completionSource =
                new TaskCompletionSource<SSCReply>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pendingReplies.TryAdd(tid, completionSource))
            {
                _tidManager.Cancel(tid);

                throw new InvalidOperationException(
                    $"TID {tid} のreply待機登録に失敗しました。");
            }

            try
            {
                byte[] frame =
                    SSCTransport.BuildFrame(
                        tid,
                        command,
                        payload,
                        destinationAddress);

                string frameHex =
                    BitConverter.ToString(frame)
                        .Replace("-", " ");

                DataReceived?.Invoke(
                    $"SSC Frame\r\n" +
                    $"TID   : {tid}\r\n" +
                    $"HEX   : {frameHex}");

                await SendHexAsync(frameHex);

                TimeSpan replyTimeout =
                    timeout ?? TimeSpan.FromSeconds(3);

                using var cancellationSource =
                    new CancellationTokenSource(replyTimeout);

                using CancellationTokenRegistration registration =
                    cancellationSource.Token.Register(
                        () => completionSource.TrySetException(
                            new TimeoutException(
                                $"SSC Reply Timeout: TID {tid}")));

                return await completionSource.Task;
            }
            finally
            {
                _pendingReplies.TryRemove(tid, out _);
                _tidManager.Cancel(tid);
            }
        }

        public async Task<byte[]> RequestPropertyPayloadAsync(string propertyName,　TimeSpan? timeout = null)
        {
            byte[] requestPayload =
                SSCTransport.BuildStringPropertyPayload(
                    propertyName);

            SSCReply reply =
                await SendSscRequestAndWaitAsync(
                    command: 0x03,
                    payload: requestPayload,
                    timeout: timeout);

            // Command 0x00が正常応答
            if (reply.Command != 0x00)
            {
                throw new InvalidOperationException(
                    $"{propertyName} の取得に失敗しました。" +
                    $" Reply Command=0x{reply.Command:X2}");
            }

            // SSCReply内部の配列を直接渡さないようコピーして返す
            return reply.Payload.ToArray();
        }

        public async Task<string> RequestStringPropertyAsync(string propertyName, TimeSpan? timeout = null)
        {
            byte[] payload =
                await RequestPropertyPayloadAsync(
                    propertyName,
                    timeout);

            // 末尾のNUL文字を除外
            int textLength = payload.Length;

            while (textLength > 0 &&
                   payload[textLength - 1] == 0x00)
            {
                textLength--;
            }

            // ASCIIではないデータを「?」へ化けさせず、
            // バイナリプロパティとして検出する
            for (int index = 0; index < textLength; index++)
            {
                byte value = payload[index];

                if (value > 0x7F)
                {
                    string payloadHex =
                        BitConverter.ToString(payload)
                            .Replace("-", " ");

                    throw new InvalidDataException(
                        $"{propertyName} はASCII文字列ではありません。" +
                        $" Payload={payloadHex}");
                }
            }

            return System.Text.Encoding.ASCII.GetString(
                payload,
                0,
                textLength);
        }

        public async Task<SscPropertyValue> RequestPropertyAsync(string propertyName, TimeSpan? timeout = null)
        {
            byte[] payload =
                await RequestPropertyPayloadAsync(
                    propertyName,
                    timeout);

            int textLength = payload.Length;

            while (textLength > 0 &&
                   payload[textLength - 1] == 0x00)
            {
                textLength--;
            }

            bool isAsciiText = true;

            for (int index = 0;
                 index < textLength;
                 index++)
            {
                byte value = payload[index];

                if (value < 0x20 ||
                    value > 0x7E)
                {
                    isAsciiText = false;
                    break;
                }
            }

            string text = isAsciiText
                ? System.Text.Encoding.ASCII.GetString(
                    payload,
                    0,
                    textLength)
                : "(バイナリデータ)";

            return new SscPropertyValue
            {
                Name = propertyName,
                Payload = (byte[])payload.Clone(),
                IsAsciiText = isAsciiText,
                Text = text
            };
        }



        public async Task<SseDeviceInformation>
        ReadDeviceInformationAsync()
        {
            string model =
                await RequestStringPropertyAsync(
                    "device.model");

            string version = "(取得不可)";
            string serial = "(取得不可)";

            try
            {
                version =
                    await RequestStringPropertyAsync(
                        "device.version");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.version : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            try
            {
                serial =
                    await RequestStringPropertyAsync(
                        "device.serial");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.serial : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            string name = "(取得不可)";

            try
            {
                name =
                    await RequestStringPropertyAsync(
                        "device.name");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.name : 取得不可\r\n" +
                    $"{ex.Message}");
            }
            string id = "(取得不可)";

            try
            {
                id =
                    await RequestStringPropertyAsync(
                        "device.id");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.id : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            string region = "(取得不可)";

            try
            {
                region =
                    await RequestStringPropertyAsync(
                        "device.region");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.region : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            string address = "(取得不可)";

            try
            {
                address =
                    await RequestStringPropertyAsync(
                        "device.address");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.address : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            string temperature = "(取得不可)";

            try
            {
                temperature =
                    await RequestStringPropertyAsync(
                        "device.temperature");
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is TimeoutException)
            {
                DataReceived?.Invoke(
                    $"device.temperature : 取得不可\r\n" +
                    $"{ex.Message}");
            }

            return new SseDeviceInformation
            {
                Model = model,
                Version = version,
                Serial = serial,
                Name = name,
                Id = id,
                Region = region,
                Address = address,
                Temperature = temperature
            };
        }
        public Task<string> RequestDeviceModelAsync()
        {
            return RequestStringPropertyAsync(
                "device.model");
        }

        public async Task<Dictionary<string, string>>
         ScanDevicePropertiesAsync()
        {
            string[] properties =
            {
                "device.model",
                "device.serial",
                "device.name",
                "device.version",
                "device.id",
                "device.region",
                "device.address",
                "device.temperature",
                "device.state",
                "device.timestamp",
                "device.build.target",
                "device.build.id",
                "device.build.hash",
                "device.build.branch",
                "device.build.state",
                "device.build.user",
                "device.build.date",
                "device.factory.progress"
            };
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string property in properties)
            {
                try
                {
                    results[property] =
                        await RequestStringPropertyAsync(property);
                }
                catch (InvalidDataException ex)
                {
                    results[property] =
                        $"バイナリ値 ({ex.Message})";
                }
                catch (InvalidOperationException ex)
                {
                    results[property] =
                        $"取得不可 ({ex.Message})";
                }
                catch (TimeoutException)
                {
                    results[property] = "タイムアウト";
                }
            }

            return results;
        }

    }
}