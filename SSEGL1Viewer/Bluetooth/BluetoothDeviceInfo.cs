namespace SSEGL1Viewer.Bluetooth
{
    internal class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "";

        public string Address { get; set; } = "";

        public ulong BluetoothAddress { get; set; }

        public short Rssi { get; set; }

        public override string ToString()
        {
            return $"{Name}    {Address}    RSSI: {Rssi} dBm";
        }
    }
}