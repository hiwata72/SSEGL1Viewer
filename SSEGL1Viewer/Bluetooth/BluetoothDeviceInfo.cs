namespace SSEGL1Viewer.Bluetooth
{
    internal class BluetoothDeviceInfo
    {
        private readonly object _syncRoot = new();

        private readonly HashSet<Guid> _serviceUuids = new();
        private readonly HashSet<string> _manufacturerData = new();

        public string Name { get; private set; } = "(名前なし)";

        public string Address { get; init; } = "";

        public ulong BluetoothAddress { get; init; }

        public short Rssi { get; private set; }

        public void UpdateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            lock (_syncRoot)
            {
                Name = name;
            }
        }

        public void UpdateRssi(short rssi)
        {
            lock (_syncRoot)
            {
                Rssi = rssi;
            }
        }

        public void AddServiceUuid(Guid uuid)
        {
            lock (_syncRoot)
            {
                _serviceUuids.Add(uuid);
            }
        }

        public void AddManufacturerData(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            lock (_syncRoot)
            {
                _manufacturerData.Add(data);
            }
        }

        public void MergeFrom(BluetoothDeviceInfo source)
        {
            UpdateName(source.Name);
            UpdateRssi(source.Rssi);

            foreach (Guid uuid in source.GetServiceUuids())
            {
                AddServiceUuid(uuid);
            }

            foreach (string data in source.GetManufacturerData())
            {
                AddManufacturerData(data);
            }
        }

        private List<Guid> GetServiceUuids()
        {
            lock (_syncRoot)
            {
                return _serviceUuids.ToList();
            }
        }

        private List<string> GetManufacturerData()
        {
            lock (_syncRoot)
            {
                return _manufacturerData.ToList();
            }
        }

        public override string ToString()
        {
            lock (_syncRoot)
            {
                string uuidText = _serviceUuids.Count == 0
                    ? "なし"
                    : string.Join(", ", _serviceUuids);

                string manufacturerText = _manufacturerData.Count == 0
                    ? "なし"
                    : string.Join(", ", _manufacturerData);

                return
                    $"{Name}    " +
                    $"{Address}    " +
                    $"RSSI: {Rssi} dBm    " +
                    $"UUID: {uuidText}    " +
                    $"MFG: {manufacturerText}";
            }
        }
    }
}