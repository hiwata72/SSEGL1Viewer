namespace SSEGL1Viewer.Bluetooth
{
    internal class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "(名前なし)";

        public string DeviceId { get; set; } = "";

        public bool IsPaired { get; set; }

        public override string ToString()
        {
            string pairedText = IsPaired
                ? "ペアリング済み"
                : "未ペアリング";

            return $"{Name}    [{pairedText}]";
        }
    }
}