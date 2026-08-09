using SSEGL1Viewer.Bluetooth;
using SSEGL1Viewer.Protocol;
using SSEGL1Viewer.Transport;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SSEGL1Viewer
{
    public partial class MainForm : Form
    {
        private readonly Queue<int> _primaryXHistory = new();
        private readonly Queue<int> _primaryYHistory = new();
        private readonly Queue<int> _primaryZHistory = new();

        private const int MaxGraphSamples = 200;

        private readonly BTManager _btManager = new();

        private const double GraphSampleIntervalSeconds = 0.01;

        private int _graphRefreshCounter;

        private const int GraphRefreshInterval = 4;

        private int _currentGraphRange = 2000;

        private ulong? _previousPacketTimestampNanoseconds;
        private uint? _previousLastSampleIndex;

        private double _measuredSampleIntervalSeconds = 0.01;

        public MainForm()
        {
            InitializeComponent();

            _btManager.DataReceived += BTManager_DataReceived;
            _btManager.MotionDecoded += BTManager_MotionDecoded;
            _btManager.ConnectionStatusChanged +=
                BTManager_ConnectionStatusChanged;

            InitializeUiState();
        }

        private void BTManager_DataReceived(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action<string>(
                        BTManager_DataReceived),
                    message);

                return;
            }

            txtReceive.AppendText(
                message +
                "\r\n\r\n");
        }

        private void InitializeUiState()
        {
            BTManager_ConnectionStatusChanged("未接続");
        }

        private void TestCrc()
        {
            SscPacket packet = Gl1Commands.CreateStartPacket();

            Debug.WriteLine($"TID Byte: 0x{packet.TransactionByte:X2}");
            Debug.WriteLine(
                $"Request Definition: 0x{packet.RequestDefinition:X2}");
            Debug.WriteLine(
                $"Payload Length: {packet.PayloadLength}");
            Debug.WriteLine(
                $"Source Address: 0x{packet.SourceAddress:X8}");
            Debug.WriteLine(
                $"Destination Address: 0x{packet.DestinationAddress:X8}");
            Debug.WriteLine($"Mode: {packet.Mode}");
            Debug.WriteLine(
                $"Payload: {Convert.ToHexString(packet.Payload)}");

            ReadOnlySpan<byte> payload = Gl1Commands.StartPayload;

            ushort crc = Crc16Ccitt.Calculate(payload);

            byte[] crcBigEndian =
            [
                (byte)(crc >> 8),
        (byte)(crc & 0xFF)
            ];

            byte[] crcLittleEndian =
            [
                (byte)(crc & 0xFF),
        (byte)(crc >> 8)
            ];


            Debug.WriteLine(
                $"CRC16: 0x{crc:X4}");

            Debug.WriteLine(
                $"CRC Big Endian: {Convert.ToHexString(crcBigEndian)}");

            Debug.WriteLine(
                $"CRC Little Endian: {Convert.ToHexString(crcLittleEndian)}");

            byte[] header = SscHeaderBuilder.Build(packet);

            Debug.WriteLine(
                $"SSC Header: {Convert.ToHexString(header)}");

            byte[] frame = SscFrameBuilder.Build(
                header,
                packet,
                CrcByteOrder.BigEndian);

            Debug.WriteLine(
                $"SSC Start Frame: {Convert.ToHexString(frame)}");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            TestCrc();
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            btnSearch.Enabled = false;

            try
            {
                string result = await _btManager.ConnectAsync();

                MessageBox.Show(
                    result,
                    "SSE-GL1 SPP接続確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "SPP接続エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSearch.Enabled = true;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _btManager.Disconnect();

            base.OnFormClosed(e);
        }


        private async void btnStartNotify_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                btnStartNotify.Enabled = false;

                await _btManager.StartGolfAsync();

                txtReceive.AppendText(
                    Environment.NewLine +
                    "Notify開始要求送信" +
                    Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Notify開始エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                // 開始に失敗した場合は接続済み状態へ戻す
                BTManager_ConnectionStatusChanged(
                    "接続済み");
            }
        }

        private async void btnStopNotify_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                btnStopNotify.Enabled = false;

                await _btManager.StopSensorAsync();

                txtReceive.AppendText(
                    Environment.NewLine +
                    "Notify停止要求完了" +
                    Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Notify停止エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                // 停止に失敗した場合はNotify中へ戻す
                BTManager_ConnectionStatusChanged(
                    "Notify受信中");
            }
        }

        private void UpdateMotionDisplay(GL1DecodedPacket packet, GL1DecodedSample sample)
        {
            lblDataId.Text =
                packet.DataId.ToString();

            lblTimestamp.Text =
                packet.Timestamp
                    .ToLocalTime()
                    .ToString(
                        "yyyy-MM-dd HH:mm:ss.fff");

            lblPrimaryX.Text =
                sample.Primary.X.ToString();

            lblPrimaryY.Text =
                sample.Primary.Y.ToString();

            lblPrimaryZ.Text =
                sample.Primary.Z.ToString();

            if (sample.Secondary.HasValue)
            {
                MotionVector3 secondary =
                    sample.Secondary.Value;

                lblSecondaryX.Text =
                    secondary.X.ToString();

                lblSecondaryY.Text =
                    secondary.Y.ToString();

                lblSecondaryZ.Text =
                    secondary.Z.ToString();
            }
            else
            {
                lblSecondaryX.Text = "-";
                lblSecondaryY.Text = "-";
                lblSecondaryZ.Text = "-";
            }
        }

        private void BTManager_ConnectionStatusChanged(string status)
        {
            Debug.WriteLine(
                $"ConnectionStatus = {status}");

            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action<string>(
                        BTManager_ConnectionStatusChanged),
                    status);

                return;
            }

            lblConnectionStatus.Text = status;

            switch (status)
            {
                case "未接続":
                    lblConnectionStatus.ForeColor =
                        Color.DimGray;

                    btnSearch.Enabled = true;
                    btnStartNotify.Enabled = false;
                    btnStopNotify.Enabled = false;
                    break;

                case "接続済み":
                    lblConnectionStatus.ForeColor =
                        Color.ForestGreen;

                    btnSearch.Enabled = false;
                    btnStartNotify.Enabled = true;
                    btnStopNotify.Enabled = false;
                    break;

                case "Notify受信中":
                    lblConnectionStatus.ForeColor =
                        Color.DodgerBlue;

                    btnSearch.Enabled = false;
                    btnStartNotify.Enabled = false;
                    btnStopNotify.Enabled = true;
                    break;

                default:
                    lblConnectionStatus.ForeColor =
                        SystemColors.ControlText;

                    btnSearch.Enabled = true;
                    btnStartNotify.Enabled = false;
                    btnStopNotify.Enabled = false;
                    break;
            }
        }
        private void pnlMotionGraph_Paint(
            object sender,
            PaintEventArgs e)
        {
            if (_primaryXHistory.Count < 2)
            {
                return;
            }

            Graphics g = e.Graphics;

            int width =
                pnlMotionGraph.ClientSize.Width;

            int height =
                pnlMotionGraph.ClientSize.Height;

            int centerY =
                height / 2;

            using Pen centerPen =
                new Pen(Color.LightGray);

            g.DrawLine(
                centerPen,
                0,
                centerY,
                width,
                centerY);

            int[] xValues =
                _primaryXHistory.ToArray();

            int[] yValues =
                _primaryYHistory.ToArray();

            int[] zValues =
                _primaryZHistory.ToArray();

            int maximumAbsoluteValue = 1;

            foreach (int value in xValues)
            {
                maximumAbsoluteValue =
                    Math.Max(
                        maximumAbsoluteValue,
                        Math.Abs(value));
            }

            foreach (int value in yValues)
            {
                maximumAbsoluteValue =
                    Math.Max(
                        maximumAbsoluteValue,
                        Math.Abs(value));
            }

            foreach (int value in zValues)
            {
                maximumAbsoluteValue =
                    Math.Max(
                        maximumAbsoluteValue,
                        Math.Abs(value));
            }

            int requiredRange =
                CalculateGraphRange(
                maximumAbsoluteValue);

            // 必要ならすぐに表示範囲を拡大する
            if (requiredRange > _currentGraphRange)
            {
                _currentGraphRange =
                    requiredRange;
            }

            int verticalRange =
                _currentGraphRange;


            float xStep =
                (float)width /
                (MaxGraphSamples - 1);

            float graphHalfHeight = height * 0.40f;

            float graphScale =
                graphHalfHeight /
                maximumAbsoluteValue;

            using Pen xPen =
                new Pen(Color.DodgerBlue, 2);

            using Pen yPen =
                new Pen(Color.ForestGreen, 2);

            using Pen zPen =
                new Pen(Color.OrangeRed, 2);

            DrawGraphLine(
                g,
                xValues,
                xStep,
                centerY,
                graphScale,
                xPen);

            DrawGraphLine(
                g,
                yValues,
                xStep,
                centerY,
                graphScale,
                yPen);

            DrawGraphLine(
                g,
                zValues,
                xStep,
                centerY,
                graphScale,
                zPen);

            using Font legendFont =
                new Font(Font.FontFamily, 9.0f, FontStyle.Bold);

            g.DrawString(
                "X",
                legendFont,
                Brushes.DodgerBlue,
                10,
                10);

            g.DrawString(
                "Y",
                legendFont,
                Brushes.ForestGreen,
                40,
                10);

            g.DrawString(
                "Z",
                legendFont,
                Brushes.OrangeRed,
                70,
                10);

            using Font axisFont =
                new Font(Font.FontFamily, 8.0f);

            const int timeDivisionCount = 4;

            for (int index = 0;
                 index <= timeDivisionCount;
                 index++)
            {
                float x =
                    width *
                    index /
                    (float)timeDivisionCount;

                double secondsFromRight =
                    (MaxGraphSamples - 1) *
                    GraphSampleIntervalSeconds *
                    (timeDivisionCount - index) /
                    timeDivisionCount;

                string timeText =
                    secondsFromRight == 0
                        ? "0 s"
                        : $"-{secondsFromRight:F1} s";

                SizeF textSize =
                    g.MeasureString(
                        timeText,
                        axisFont);

                g.DrawString(
                    timeText,
                    axisFont,
                    Brushes.DimGray,
                    x - textSize.Width / 2,
                    height - textSize.Height - 4);


                string upperText =
                    $"+{verticalRange}";

                string zeroText =
                    "0";

                string lowerText =
                    $"-{verticalRange}";

                g.DrawString(
                    upperText,
                    axisFont,
                    Brushes.DimGray,
                    5,
                    height * 0.10f);

                g.DrawString(
                    zeroText,
                    axisFont,
                    Brushes.DimGray,
                    5,
                    centerY);

                g.DrawString(
                    lowerText,
                    axisFont,
                    Brushes.DimGray,
                    5,
                    height * 0.90f - 15);
            }
        }

        private static void DrawGraphLine(Graphics g, int[] values, float xStep, int centerY, float graphScale, Pen pen)
        {
            for (int index = 1;
                 index < values.Length;
                 index++)
            {
                float x1 =
                    (index - 1) * xStep;

                float y1 =
                    centerY -
                    values[index - 1] * graphScale;

                float x2 =
                    index * xStep;

                float y2 =
                    centerY -
                    values[index] * graphScale;

                g.DrawLine(
                    pen,
                    x1,
                    y1,
                    x2,
                    y2);
            }
        }

        private void BTManager_MotionDecoded(
    GL1DecodedPacket packet)
        {
            if (packet.Samples.Count == 0)
            {
                return;
            }

            GL1DecodedSample latestSample =
                packet.Samples[^1];

            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        () =>
                        {
                            UpdateMotionDisplay(
                                packet,
                                latestSample);

                            if (packet.DataId == 0)
                            {
                                foreach (GL1DecodedSample sample
                                         in packet.Samples)
                                {
                                    AddGraphSample(
                                        sample.Primary.X,
                                        sample.Primary.Y,
                                        sample.Primary.Z);
                                }

                                _graphRefreshCounter++;

                                if (_graphRefreshCounter >=
                                    GraphRefreshInterval)
                                {
                                    _graphRefreshCounter = 0;

                                    pnlMotionGraph.Invalidate();
                                }
                            }
                        }));

                return;
            }

            UpdateMotionDisplay(
                packet,
                latestSample);

            if (_previousPacketTimestampNanoseconds.HasValue &&
                _previousLastSampleIndex.HasValue)
            {
                // 現在は未使用
            }

            _previousPacketTimestampNanoseconds =
                packet.TimestampNanoseconds;

            _previousLastSampleIndex =
                packet.Samples[^1].Index;

            Debug.WriteLine(
                $"DataId={packet.DataId}, " +
                $"Delta={packet.IsDeltaTime}, " +
                $"TimeNs={packet.TimestampNanoseconds}, " +
                $"Samples={packet.Samples.Count}, " +
                $"FirstIndex={packet.Samples[0].Index}, " +
                $"LastIndex={packet.Samples[^1].Index}");

            if (packet.DataId == 0)
            {
                foreach (GL1DecodedSample sample
                         in packet.Samples)
                {
                    AddGraphSample(
                        sample.Primary.X,
                        sample.Primary.Y,
                        sample.Primary.Z);
                }

                _graphRefreshCounter++;

                if (_graphRefreshCounter >=
                    GraphRefreshInterval)
                {
                    _graphRefreshCounter = 0;

                    pnlMotionGraph.Invalidate();
                }
            }
        }

        private void AddGraphSample(
            int x,
            int y,
            int z)
        {
            _primaryXHistory.Enqueue(x);
            _primaryYHistory.Enqueue(y);
            _primaryZHistory.Enqueue(z);

            while (_primaryXHistory.Count > MaxGraphSamples)
            {
                _primaryXHistory.Dequeue();
            }

            while (_primaryYHistory.Count > MaxGraphSamples)
            {
                _primaryYHistory.Dequeue();
            }

            while (_primaryZHistory.Count > MaxGraphSamples)
            {
                _primaryZHistory.Dequeue();
            }
        }

        private static int CalculateGraphRange(
            int maximumAbsoluteValue)
        {
            if (maximumAbsoluteValue <= 100)
            {
                return 100;
            }

            if (maximumAbsoluteValue <= 250)
            {
                return 250;
            }

            if (maximumAbsoluteValue <= 500)
            {
                return 500;
            }

            if (maximumAbsoluteValue <= 1000)
            {
                return 1000;
            }

            if (maximumAbsoluteValue <= 2000)
            {
                return 2000;
            }

            if (maximumAbsoluteValue <= 5000)
            {
                return 5000;
            }

            if (maximumAbsoluteValue <= 10000)
            {
                return 10000;
            }

            return
                ((maximumAbsoluteValue + 9999) /
                 10000) *
                10000;
        }
    }
}



