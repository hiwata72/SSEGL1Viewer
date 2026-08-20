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

        private readonly Queue<double> _accelAngleXHistory =
            new Queue<double>();

        private readonly Queue<double> _fusedAngleXHistory =
            new Queue<double>();

        private readonly Queue<double> _accelAngleYHistory =
            new Queue<double>();

        private readonly Queue<double> _fusedAngleYHistory =
            new Queue<double>();

        private readonly Queue<double> _fusedAngleZHistory =
            new Queue<double>();

        private readonly Queue<int> _gyroXHistory = new();
        private readonly Queue<int> _gyroYHistory = new();
        private readonly Queue<int> _gyroZHistory = new();



        private const int MaxGraphSamples = 8000;

        private readonly BTManager _btManager = new();

        //private const double GraphSampleIntervalSeconds =
        //    0.00127135;

        private const double DataId0SampleIntervalSeconds =
            0.00127135;

        private int _graphRefreshCounter;

        private const int GraphRefreshInterval = 4;

        private int _currentGraphRange = 2000;

        private ulong? _previousDataId0Timestamp;

        private const double AccelCountsPerG = 2048.0;

        private readonly System.Windows.Forms.Timer _graphTimer =
            new System.Windows.Forms.Timer();

        private readonly object _motionDataLock =
            new object();

        private GL1DecodedPacket? _latestMotionPacket;
        private GL1DecodedSample? _latestMotionSample;

        private double _gyroIntegralX = 0.0;
        private double _gyroIntegralY = 0.0;
        private double _gyroIntegralZ = 0.0;

        private double _gyroBiasX = 0.0;
        private double _gyroBiasY = 0.0;
        private double _gyroBiasZ = 0.0;

        private const double GyroSampleIntervalSeconds = 0.0012713;

        private bool _isGyroBiasMeasuring = false;

        private long _gyroBiasSampleCount = 0;

        private double _gyroBiasSumX = 0.0;
        private double _gyroBiasSumY = 0.0;
        private double _gyroBiasSumZ = 0.0;

        private const int GyroBiasTargetSamples = 1600;

        private bool _isGyroAngleMeasuring = false;

        // SSE-GL1 実機90度回転試験による暫定値
        // 1 count ≈ 0.0572 deg/s
        private const double GyroScaleDegPerSecondPerCount =
            0.0572;

        private double _gyroAngleX = 0.0;
        private double _gyroAngleY = 0.0;
        private double _gyroAngleZ = 0.0;

        private double _fusedAngleX;
        private double _fusedAngleY;
        private double _fusedAngleZ;

        // ★ 角度ゼロ基準
        private double _angleZeroX;
        private double _angleZeroY;
        private double _angleZeroZ;

        private bool _isFusedAngleInitialized;

        private const double ComplementaryAlpha =
            0.98;

        private MotionVector3? _latestGyroSample;

        private MotionVector3? _latestAccelSample;

        public MainForm()
        {
            InitializeComponent();

            _graphTimer.Interval = 50;   // 50ms = 約20fps

            _graphTimer.Tick += GraphTimer_Tick;

            _graphTimer.Start();

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
            _currentGraphRange = 2000;
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



        private void UpdateMotionDisplay(
    GL1DecodedPacket packet,
    GL1DecodedSample sample)
        {
            // ★ 加速度表示
            MotionVector3 accel =
                sample.Primary;

            const double AccelCountsPerG =
                2048.0;

            double accelXG =
                accel.X /
                AccelCountsPerG;

            double accelYG =
                accel.Y /
                AccelCountsPerG;

            double accelZG =
                accel.Z /
                AccelCountsPerG;

            lblPrimaryX.Text =
                $"{accelXG:+0.000;-0.000;0.000} g";

            lblPrimaryY.Text =
                $"{accelYG:+0.000;-0.000;0.000} g";

            lblPrimaryZ.Text =
                $"{accelZG:+0.000;-0.000;0.000} g";

            // この下に既存の表示処理があればそのまま残す
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

            /*
             * 中央線
             */
            using Pen centerPen =
                new Pen(Color.LightGray);

            g.DrawLine(
                centerPen,
                0,
                centerY,
                width,
                centerY);

            /*
             * 履歴取得
             */
            int[] xValues;
            int[] yValues;
            int[] zValues;

            lock (_motionDataLock)
            {
                xValues =
                    _primaryXHistory.ToArray();

                yValues =
                    _primaryYHistory.ToArray();

                zValues =
                    _primaryZHistory.ToArray();
            }
            /*
             * 現在履歴中の最大絶対値
             */
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

            /*
             * 縦軸レンジ決定
             */
            int requiredRange =
                CalculateGraphRange(
                    maximumAbsoluteValue);

            if (requiredRange > _currentGraphRange)
            {
                _currentGraphRange =
                    requiredRange;
            }

            int verticalRange =
                _currentGraphRange;

            /*
             * 描画倍率
             */
            float graphHalfHeight =
                height * 0.40f;

            // ★重要
            // maximumAbsoluteValueではなく
            // 縦軸表示と同じverticalRangeを使う
            float graphScale =
                graphHalfHeight /
                verticalRange;

            /*
             * 横方向
             */
            float xStep =
                (float)width /
                (MaxGraphSamples - 1);

            /*
             * 波形描画
             */
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

            /*
             * X/Y/Z 凡例
             */
            using Font legendFont =
                new Font(
                    Font.FontFamily,
                    9.0f,
                    FontStyle.Bold);

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

            /*
             * 軸文字
             */
            using Font axisFont =
                new Font(
                    Font.FontFamily,
                    8.0f);

            /*
             * 横軸（時間）
             */
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
                    DataId0SampleIntervalSeconds *
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
            }

            /*
             * 縦軸
             * ★時間目盛りforループの外で1回だけ描く
             */
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

        private static void DrawGraphLine(
            Graphics g,
            int[] values,
            float xStep,
            int centerY,
            float graphScale,
            Pen pen)
        {
            int sampleCount =
                values.Length;

            int startIndex =
                Math.Max(
                    0,
                    MaxGraphSamples - sampleCount);

            for (int index = 1;
                 index < sampleCount;
                 index++)
            {
                float x1 =
                    (startIndex + index - 1) *
                    xStep;

                float y1 =
                    centerY -
                    values[index - 1] *
                    graphScale;

                float x2 =
                    (startIndex + index) *
                    xStep;

                float y2 =
                    centerY -
                    values[index] *
                    graphScale;

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

            // DataId=0のみ使用
            if (packet.DataId != 0)
            {
                return;
            }

            GL1DecodedSample latestSample =
                packet.Samples[^1];

            lock (_motionDataLock)
            {
                // 最新値を保存
                _latestMotionPacket =
                    packet;

                _latestMotionSample =
                    latestSample;

                // 全サンプルを履歴へ追加
                foreach (GL1DecodedSample sample
                         in packet.Samples)
                {
                    // ★ 最新加速度を保存
                    _latestAccelSample =
                        sample.Primary;

                    AddGraphSample(
                        sample.Primary.X,
                        sample.Primary.Y,
                        sample.Primary.Z);

                    // ★ 加速度から傾斜角を計算
                    CalculateAccelAngles(
                        sample.Primary.X,
                        sample.Primary.Y,
                        sample.Primary.Z,
                        out double accelAngleX,
                        out double accelAngleY);

                    if (sample.Secondary.HasValue)
                        if (sample.Secondary.HasValue)
                        {
                            MotionVector3 gyro =
                                sample.Secondary.Value;

                            // ★ 最新ジャイロ値を保存
                            _latestGyroSample =
                                gyro;

                            AddGyroGraphSample(
                                gyro.X,
                                gyro.Y,
                                gyro.Z);

                            // ★ コンプリメンタリフィルタ
                            double gyroRateX =
                                (gyro.X - _gyroBiasX) *
                                GyroScaleDegPerSecondPerCount;

                            double gyroRateY =
                                (gyro.Y - _gyroBiasY) *
                                GyroScaleDegPerSecondPerCount;

                            double gyroRateZ =
                                (gyro.Z - _gyroBiasZ) *
                                GyroScaleDegPerSecondPerCount;

                            if (!_isFusedAngleInitialized)
                            {
                                // 最初だけ加速度角を初期値にする
                                _fusedAngleX =
                                    accelAngleX;

                                _fusedAngleY =
                                    accelAngleY;

                                _isFusedAngleInitialized =
                                        true;
                            }
                            else
                            {
                                _fusedAngleX =
                                    ComplementaryAlpha *
                                    (_fusedAngleX +
                                     gyroRateX *
                                     GyroSampleIntervalSeconds) +
                                    (1.0 - ComplementaryAlpha) *
                                    accelAngleX;

                                _fusedAngleY =
                                    ComplementaryAlpha *
                                    (_fusedAngleY +
                                    gyroRateY *
                                    GyroSampleIntervalSeconds) +
                                    (1.0 - ComplementaryAlpha) *
                                    accelAngleY;

                                // ★ Z角は加速度で補正できないため
                                //    ジャイロ角速度を常時積分
                                _fusedAngleZ +=
                                    gyroRateZ *
                                    GyroSampleIntervalSeconds;

                            }

                            // ★ 角度比較グラフ用履歴
                            AddAngleGraphSample(accelAngleX, accelAngleY, _fusedAngleX, _fusedAngleY);



                            if (_isGyroBiasMeasuring)
                            {
                                _gyroBiasSumX += gyro.X;
                                _gyroBiasSumY += gyro.Y;
                                _gyroBiasSumZ += gyro.Z;

                                _gyroBiasSampleCount++;

                                if (_gyroBiasSampleCount % 200 == 0)
                                {
                                    Debug.WriteLine(
                                        $"Gyro Bias Measuring: " +
                                        $"{_gyroBiasSampleCount}/" +
                                        $"{GyroBiasTargetSamples}");
                                }



                                if (_gyroBiasSampleCount >=
                                    GyroBiasTargetSamples)
                                {
                                    _gyroBiasX =
                                        _gyroBiasSumX /
                                        _gyroBiasSampleCount;

                                    _gyroBiasY =
                                        _gyroBiasSumY /
                                        _gyroBiasSampleCount;

                                    _gyroBiasZ =
                                        _gyroBiasSumZ /
                                        _gyroBiasSampleCount;

                                    _isGyroBiasMeasuring = false;

                                    Debug.WriteLine(
                                        $"Gyro Bias Complete: " +
                                        $"Samples={_gyroBiasSampleCount}, " +
                                        $"X={_gyroBiasX:F3}, " +
                                        $"Y={_gyroBiasY:F3}, " +
                                        $"Z={_gyroBiasZ:F3}");
                                }
                            }

                            if (_isGyroAngleMeasuring)
                            {
                                double correctedX =
                                    gyro.X - _gyroBiasX;

                                double correctedY =
                                    gyro.Y - _gyroBiasY;

                                double correctedZ =
                                    gyro.Z - _gyroBiasZ;

                                _gyroIntegralX +=
                                    correctedX *
                                    GyroSampleIntervalSeconds;

                                _gyroIntegralY +=
                                    correctedY *
                                    GyroSampleIntervalSeconds;

                                _gyroIntegralZ +=
                                    correctedZ *
                                    GyroSampleIntervalSeconds;

                                double correctedGyroX =
                                    gyro.X - _gyroBiasX;

                                double correctedGyroY =
                                    gyro.Y - _gyroBiasY;

                                double correctedGyroZ =
                                    gyro.Z - _gyroBiasZ;

                                _gyroAngleX +=
                                    correctedGyroX *
                                    GyroScaleDegPerSecondPerCount *
                                    GyroSampleIntervalSeconds;

                                _gyroAngleY +=
                                    correctedGyroY *
                                    GyroScaleDegPerSecondPerCount *
                                    GyroSampleIntervalSeconds;

                                _gyroAngleZ +=
                                    correctedGyroZ *
                                    GyroScaleDegPerSecondPerCount *
                                    GyroSampleIntervalSeconds;

                            }

                        }

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

        private void AddAngleGraphSample(
            double accelAngleX,
            double accelAngleY,
            double fusedAngleX,
            double fusedAngleY)
        {
            _accelAngleXHistory.Enqueue(
                accelAngleX);

            _accelAngleYHistory.Enqueue(
                accelAngleY);

            _fusedAngleXHistory.Enqueue(
                fusedAngleX);

            _fusedAngleYHistory.Enqueue(
                fusedAngleY);

            while (_accelAngleXHistory.Count >
                   MaxGraphSamples)
            {
                _accelAngleXHistory.Dequeue();
            }

            while (_accelAngleYHistory.Count >
                   MaxGraphSamples)
            {
                _accelAngleYHistory.Dequeue();
            }

            while (_fusedAngleXHistory.Count >
                   MaxGraphSamples)
            {
                _fusedAngleXHistory.Dequeue();
            }

            while (_fusedAngleYHistory.Count >
                   MaxGraphSamples)
            {
                _fusedAngleYHistory.Dequeue();
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

        private async void btnRfcommTest_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                btnRfcommTest.Enabled =
                    false;

                string result =
                    await _btManager
                        .DumpRfcommServicesAsync();

                MessageBox.Show(
                    result,
                    "RFCOMMサービス確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "RFCOMM確認エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnRfcommTest.Enabled =
                    true;
            }
        }

        private void AddGyroGraphSample(
            int x,
            int y,
            int z)
        {
            _gyroXHistory.Enqueue(x);
            _gyroYHistory.Enqueue(y);
            _gyroZHistory.Enqueue(z);

            while (_gyroXHistory.Count > MaxGraphSamples)
            {
                _gyroXHistory.Dequeue();
            }

            while (_gyroYHistory.Count > MaxGraphSamples)
            {
                _gyroYHistory.Dequeue();
            }

            while (_gyroZHistory.Count > MaxGraphSamples)
            {
                _gyroZHistory.Dequeue();
            }
        }

        private void pnlGyroGraph_Paint(
    object sender,
    PaintEventArgs e)
        {
            if (_gyroXHistory.Count < 2)
            {
                return;
            }

            Graphics g = e.Graphics;

            int width =
                pnlGyroGraph.ClientSize.Width;

            int height =
                pnlGyroGraph.ClientSize.Height;

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

            int[] xValues;
            int[] yValues;
            int[] zValues;

            lock (_motionDataLock)
            {
                xValues =
                    _gyroXHistory.ToArray();

                yValues =
                    _gyroYHistory.ToArray();

                zValues =
                    _gyroZHistory.ToArray();
            }

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

            int verticalRange =
                CalculateGraphRange(
                    maximumAbsoluteValue);

            float graphHalfHeight =
                height * 0.40f;

            float graphScale =
                graphHalfHeight /
                verticalRange;

            float xStep =
                (float)width /
                (MaxGraphSamples - 1);

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
                new Font(
                    Font.FontFamily,
                    9.0f,
                    FontStyle.Bold);

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
                new Font(
                    Font.FontFamily,
                    8.0f);

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



        private void GraphTimer_Tick(
            object? sender,
            EventArgs e)
        {
            double angleZeroX;
            double angleZeroY;
            double angleZeroZ;

            MotionVector3? latestAccel;

            GL1DecodedPacket? packet;
            GL1DecodedSample? sample;
            MotionVector3? latestGyro;

            lock (_motionDataLock)
            {
                packet =
                    _latestMotionPacket;

                sample =
                    _latestMotionSample;

                latestAccel =
                    _latestAccelSample;

                latestGyro =
                    _latestGyroSample;

                angleZeroX =
                    _angleZeroX;

                angleZeroY =
                    _angleZeroY;

                angleZeroZ =
                    _angleZeroZ;
            }


            if (latestAccel.HasValue)
            {
                MotionVector3 accel =
                    latestAccel.Value;

                const double AccelCountsPerG =
                    2048.0;

                double accelXG =
                    accel.X /
                    AccelCountsPerG;

                double accelYG =
                    accel.Y /
                    AccelCountsPerG;

                double accelZG =
                    accel.Z /
                    AccelCountsPerG;

                lblPrimaryX.Text =
                    $"{accelXG:+0.000;-0.000;0.000} g";

                lblPrimaryY.Text =
                    $"{accelYG:+0.000;-0.000;0.000} g";

                lblPrimaryZ.Text =
                    $"{accelZG:+0.000;-0.000;0.000} g";

                CalculateAccelAngles(
                    accel.X,
                    accel.Y,
                    accel.Z,
                    out double accelAngleX,
                    out double accelAngleY);

                // ★ 加速度角を画面表示
                lblAccelAngleX.Text =
                    $"{accelAngleX:+0.0;-0.0;0.0} °";

                lblAccelAngleY.Text =
                    $"{accelAngleY:+0.0;-0.0;0.0} °";

                Debug.WriteLine(
                    $"Accel Angle: " +
                    $"X={accelAngleX:F1} deg, " +
                    $"Y={accelAngleY:F1} deg");
            }
            else
            {
                lblPrimaryX.Text = "-";
                lblPrimaryY.Text = "-";
                lblPrimaryZ.Text = "-";

                lblAccelAngleX.Text = "-";
                lblAccelAngleY.Text = "-";
            }


            // ★ ジャイロ瞬時値表示
            if (latestGyro.HasValue)
            {
                MotionVector3 gyro =
                    latestGyro.Value;

                double gyroXDegPerSecond =
                    (gyro.X - _gyroBiasX) *
                    GyroScaleDegPerSecondPerCount;

                double gyroYDegPerSecond =
                    (gyro.Y - _gyroBiasY) *
                    GyroScaleDegPerSecondPerCount;

                double gyroZDegPerSecond =
                    (gyro.Z - _gyroBiasZ) *
                    GyroScaleDegPerSecondPerCount;

                lblSecondaryX.Text =
                    $"{gyroXDegPerSecond:+0.0;-0.0;0.0} °/s";

                lblSecondaryY.Text =
                    $"{gyroYDegPerSecond:+0.0;-0.0;0.0} °/s";

                lblSecondaryZ.Text =
                    $"{gyroZDegPerSecond:+0.0;-0.0;0.0} °/s";
            }
            else
            {
                lblSecondaryX.Text = "-";
                lblSecondaryY.Text = "-";
                lblSecondaryZ.Text = "-";
            }

            pnlMotionGraph.Invalidate();
            pnlGyroGraph.Invalidate();
            pnlAngleGraph.Invalidate();

            double angleX;
            double angleY;
            double angleZ;

            double fusedAngleX;
            double fusedAngleY;
            double fusedAngleZ;

            bool fusedAngleInitialized;

            lock (_motionDataLock)
            {
                angleX =
                    _gyroAngleX;

                angleY =
                    _gyroAngleY;

                angleZ =
                    _gyroAngleZ;

                fusedAngleX =
                    _fusedAngleX;

                fusedAngleY =
                    _fusedAngleY;

                fusedAngleZ =
                    _fusedAngleZ;

                fusedAngleInitialized =
                    _isFusedAngleInitialized;
            }

            lblAngleX.Text =
                $"{angleX:+0.0;-0.0;0.0} °";

            lblAngleY.Text =
                $"{angleY:+0.0;-0.0;0.0} °";

            lblAngleZ.Text =
                $"{angleZ:+0.0;-0.0;0.0} °";

            double relativeFusedAngleX =
                fusedAngleX -
                angleZeroX;

            double relativeFusedAngleY =
                fusedAngleY -
                angleZeroY;

            double relativeFusedAngleZ =
                fusedAngleZ -
                angleZeroZ;

            // ★ 融合角表示
            if (fusedAngleInitialized)
            {
                lblFusedAngleX.Text =
                    $"{relativeFusedAngleX:+0.0;-0.0;0.0} °";

                lblFusedAngleY.Text =
                    $"{relativeFusedAngleY:+0.0;-0.0;0.0} °";

                lblFusedAngleZ.Text =
                    $"{relativeFusedAngleZ:+0.0;-0.0;0.0} °";
            }
            else
            {
                lblFusedAngleX.Text =
                    "-";

                lblFusedAngleY.Text =
                    "-";

                lblFusedAngleZ.Text =
                    "-";
            }
        }

        private void StartGyroBiasMeasurement()
        {
            lock (_motionDataLock)
            {
                _gyroBiasSampleCount = 0;

                _gyroBiasSumX = 0.0;
                _gyroBiasSumY = 0.0;
                _gyroBiasSumZ = 0.0;

                _isGyroBiasMeasuring = true;
            }

            Debug.WriteLine(
                "Gyro Bias Measurement Start");
        }

        private void btnGyroBias_Click(
            object sender,
            EventArgs e)
        {
            if (_isGyroBiasMeasuring)
            {
                MessageBox.Show(
                    "現在ジャイロ零点を測定中です。",
                    "ジャイロ零点測定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            StartGyroBiasMeasurement();

            MessageBox.Show(
                "センサーを動かさずに約2秒間静止させてください。",
                "ジャイロ零点測定",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void StartGyroAngleMeasurement()
        {
            lock (_motionDataLock)
            {
                _gyroIntegralX = 0.0;
                _gyroIntegralY = 0.0;
                _gyroIntegralZ = 0.0;

                _isGyroAngleMeasuring = true;
            }

            Debug.WriteLine(
                "Gyro Angle Measurement Start");
        }


        private void StopGyroAngleMeasurement()
        {
            double angleX;
            double angleY;
            double angleZ;

            lock (_motionDataLock)
            {
                _isGyroAngleMeasuring = false;

                angleX =
                    _gyroIntegralX *
                    GyroScaleDegPerSecondPerCount;

                angleY =
                    _gyroIntegralY *
                    GyroScaleDegPerSecondPerCount;

                angleZ =
                    _gyroIntegralZ *
                    GyroScaleDegPerSecondPerCount;
            }

            Debug.WriteLine(
                $"Gyro Angle Measurement Stop: " +
                $"X={angleX:F1} deg, " +
                $"Y={angleY:F1} deg, " +
                $"Z={angleZ:F1} deg");
        }



        private void btnGyroAngleStart_Click(
            object sender,
            EventArgs e)
        {
            StartGyroAngleMeasurement();
        }

        private void btnGyroAngleStop_Click(
            object sender,
            EventArgs e)
        {
            StopGyroAngleMeasurement();
        }

        private void btnAngleReset_Click(
            object? sender,
            EventArgs e)
        {
            lock (_motionDataLock)
            {
                // ★ 現在のX/Y姿勢をゼロ基準として保存
                _angleZeroX =
                    _fusedAngleX;

                _angleZeroY =
                    _fusedAngleY;

                // ★ Zは現在位置を0°基準にする
                _angleZeroZ =
                    _fusedAngleZ;

                // 従来のジャイロ角測定値もリセット
                _gyroAngleX = 0.0;
                _gyroAngleY = 0.0;
                _gyroAngleZ = 0.0;

                _gyroIntegralX = 0.0;
                _gyroIntegralY = 0.0;
                _gyroIntegralZ = 0.0;

                // ★ _fusedAngleX/Y/Z はここでは0にしない
                // ★ _isFusedAngleInitialized も false にしない

                _accelAngleXHistory.Clear();
                _fusedAngleXHistory.Clear();

                _accelAngleYHistory.Clear();
                _fusedAngleYHistory.Clear();
            }
            pnlAngleGraph.Invalidate();

            Debug.WriteLine(
                "Angle reset: Gyro/Fused/History cleared.");
        }

        private static void CalculateAccelAngles(
    double ax,
    double ay,
    double az,
    out double angleX,
    out double angleY)
        {
            angleX =
                Math.Atan2(
                    ay,
                    Math.Sqrt(
                        ax * ax +
                        az * az)) *
                180.0 /
                Math.PI;

            angleY =
                Math.Atan2(
                    -ax,
                    Math.Sqrt(
                        ay * ay +
                        az * az)) *
                180.0 /
                Math.PI;
        }

        private static void DrawAngleGraphLine(
           Graphics g,
           double[] values,
           float xStep,
           int centerY,
           float graphScale,
           Pen pen)
        {
            if (values.Length < 2)
            {
                return;
            }

            // ★ 最新データが常に右端になるように開始位置を調整
            float startX =
                g.VisibleClipBounds.Width -
                (values.Length - 1) *
                xStep;

            for (int index = 1;
                 index < values.Length;
                 index++)
            {
                float x1 =
                    startX +
                    (index - 1) *
                    xStep;

                float y1 =
                    centerY -
                    (float)values[index - 1] *
                    graphScale;

                float x2 =
                    startX +
                    index *
                    xStep;

                float y2 =
                    centerY -
                    (float)values[index] *
                    graphScale;

                g.DrawLine(
                    pen,
                    x1,
                    y1,
                    x2,
                    y2);
            }
        }
        private void pnlAngleGraph_Paint(
            object? sender,
            PaintEventArgs e)
        {
            using Pen accelXPen =
                new Pen(
                    Color.DodgerBlue,
                    2);

            using Pen fusedXPen =
                new Pen(
                    Color.OrangeRed,
                    2);

            using Pen accelYPen =
                new Pen(
                    Color.ForestGreen,
                    2);

            using Pen fusedYPen =
                new Pen(
                    Color.DarkOrange,
                    2);


            if (_accelAngleXHistory.Count < 2 ||
                _fusedAngleXHistory.Count < 2)
            {
                return;
            }

            Graphics g =
                e.Graphics;

            int width =
                pnlAngleGraph.ClientSize.Width;

            int height =
                pnlAngleGraph.ClientSize.Height;

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

            double[] accelXValues;
            double[] fusedXValues;

            double[] accelYValues;
            double[] fusedYValues;

            lock (_motionDataLock)
            {
                accelXValues =
                    _accelAngleXHistory.ToArray();

                fusedXValues =
                    _fusedAngleXHistory.ToArray();

                accelYValues =
                    _accelAngleYHistory.ToArray();

                fusedYValues =
                    _fusedAngleYHistory.ToArray();
            }

            const double verticalRange =
                90.0;

            float graphHalfHeight =
                height * 0.40f;

            float graphScale =
                graphHalfHeight /
                (float)verticalRange;

            float xStep =
                (float)width /
                (MaxGraphSamples - 1);

            using Pen accelPen =
                new Pen(
                    Color.DodgerBlue,
                    2);

            using Pen fusedPen =
                new Pen(
                    Color.OrangeRed,
                    2);

            DrawAngleGraphLine(
                g,
                accelXValues,
                xStep,
                centerY,
                graphScale,
                accelXPen);

            DrawAngleGraphLine(
                g,
                fusedXValues,
                xStep,
                centerY,
                graphScale,
                fusedXPen);

            DrawAngleGraphLine(
                g,
                accelYValues,
                xStep,
                centerY,
                graphScale,
                accelYPen);

            DrawAngleGraphLine(
                g,
                fusedYValues,
                xStep,
                centerY,
                graphScale,
                fusedYPen);

            using Font legendFont =
                new Font(
                    Font.FontFamily,
                    9.0f,
                    FontStyle.Bold);

            g.DrawString(
                "Accel X",
                legendFont,
                Brushes.DodgerBlue,
                10,
                10);

            g.DrawString(
                "Fused X",
                legendFont,
                Brushes.OrangeRed,
                90,
                10);

            g.DrawString(
                "Accel Y",
                legendFont,
                Brushes.ForestGreen,
                170,
                10);

            g.DrawString(
                "Fused Y",
                legendFont,
                Brushes.DarkOrange,
                250,
                10);

            using Font axisFont =
                new Font(
                    Font.FontFamily,
                    8.0f);

            g.DrawString(
                "+90°",
                axisFont,
                Brushes.DimGray,
                5,
                height * 0.10f);

            g.DrawString(
                "0°",
                axisFont,
                Brushes.DimGray,
                5,
                centerY);

            g.DrawString(
                "-90°",
                axisFont,
                Brushes.DimGray,
                5,
                height * 0.90f - 15);
        }

        private async void btnRfcommConnectTest_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                btnRfcommConnectTest.Enabled =
                    false;

                string result =
                    await _btManager
                        .TestRfcommConnectAsync();

                MessageBox.Show(
                    result,
                    "RFCOMM接続確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "RFCOMM接続確認エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnRfcommConnectTest.Enabled =
                    true;
            }
        }
    }
}



