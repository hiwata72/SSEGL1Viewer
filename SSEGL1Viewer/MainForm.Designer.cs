namespace SSEGL1Viewer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtReceive = new TextBox();
            grpConnection = new GroupBox();
            lstDevices = new ListBox();
            label6 = new Label();
            label3 = new Label();
            lblConnectionStatus = new Label();
            lblTimestamp = new Label();
            lblDataId = new Label();
            label5 = new Label();
            label4 = new Label();
            lblDeviceName = new Label();
            label2 = new Label();
            btnStopNotify = new Button();
            btnStartNotify = new Button();
            btnSearch = new Button();
            grpAcceleration = new GroupBox();
            lblPrimaryZ = new Label();
            lblPrimaryY = new Label();
            lblPrimaryX = new Label();
            label8 = new Label();
            label7 = new Label();
            label1 = new Label();
            grpGyroscope = new GroupBox();
            label19 = new Label();
            label20 = new Label();
            label18 = new Label();
            lblFusedAngleZ = new Label();
            label16 = new Label();
            label17 = new Label();
            lblFusedAngleY = new Label();
            lblFusedAngleX = new Label();
            label15 = new Label();
            lblAccelAngleY = new Label();
            lblAccelAngleX = new Label();
            lblAccelAngleTitle = new Label();
            label12 = new Label();
            label13 = new Label();
            label14 = new Label();
            lblAngleZ = new Label();
            lblAngleY = new Label();
            lblAngleX = new Label();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            lblSecondaryZ = new Label();
            lblSecondaryY = new Label();
            lblSecondaryX = new Label();
            pnlMotionGraph = new DoubleBufferedPanel();
            lblGraphPlaceholder = new Label();
            grpDebugLog = new GroupBox();
            btnRfcommTest = new Button();
            pnlGyroGraph = new DoubleBufferedPanel();
            btnGyroBias = new Button();
            btnGyroAngleStart = new Button();
            btnGyroAngleStop = new Button();
            btnAngleReset = new Button();
            pnlAngleGraph = new DoubleBufferedPanel();
            btnRfcommConnectTest = new Button();
            btnRfcommReceiveTest = new Button();
            grpConnection.SuspendLayout();
            grpAcceleration.SuspendLayout();
            grpGyroscope.SuspendLayout();
            pnlMotionGraph.SuspendLayout();
            grpDebugLog.SuspendLayout();
            SuspendLayout();
            // 
            // txtReceive
            // 
            txtReceive.Location = new Point(12, 35);
            txtReceive.Multiline = true;
            txtReceive.Name = "txtReceive";
            txtReceive.ReadOnly = true;
            txtReceive.ScrollBars = ScrollBars.Both;
            txtReceive.Size = new Size(1019, 83);
            txtReceive.TabIndex = 2;
            txtReceive.WordWrap = false;
            // 
            // grpConnection
            // 
            grpConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpConnection.Controls.Add(lstDevices);
            grpConnection.Controls.Add(label6);
            grpConnection.Controls.Add(label3);
            grpConnection.Controls.Add(lblConnectionStatus);
            grpConnection.Controls.Add(lblTimestamp);
            grpConnection.Controls.Add(lblDataId);
            grpConnection.Controls.Add(label5);
            grpConnection.Controls.Add(label4);
            grpConnection.Controls.Add(lblDeviceName);
            grpConnection.Controls.Add(label2);
            grpConnection.Controls.Add(btnStopNotify);
            grpConnection.Controls.Add(btnStartNotify);
            grpConnection.Controls.Add(btnSearch);
            grpConnection.Location = new Point(20, 15);
            grpConnection.Name = "grpConnection";
            grpConnection.Size = new Size(1052, 117);
            grpConnection.TabIndex = 19;
            grpConnection.TabStop = false;
            grpConnection.Text = "接続・操作";
            // 
            // lstDevices
            // 
            lstDevices.FormattingEnabled = true;
            lstDevices.Location = new Point(424, 40);
            lstDevices.Name = "lstDevices";
            lstDevices.Size = new Size(608, 64);
            lstDevices.TabIndex = 22;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(383, 22);
            label6.Name = "label6";
            label6.Size = new Size(55, 15);
            label6.TabIndex = 21;
            label6.Text = "機器一覧";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(187, 55);
            label3.Name = "label3";
            label3.Size = new Size(56, 15);
            label3.TabIndex = 20;
            label3.Text = "デバイス：";
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Location = new Point(92, 55);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(43, 15);
            lblConnectionStatus.TabIndex = 19;
            lblConnectionStatus.Text = "未接続";
            // 
            // lblTimestamp
            // 
            lblTimestamp.AutoSize = true;
            lblTimestamp.Location = new Point(217, 81);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.Size = new Size(12, 15);
            lblTimestamp.TabIndex = 18;
            lblTimestamp.Text = "-";
            // 
            // lblDataId
            // 
            lblDataId.AutoSize = true;
            lblDataId.Location = new Point(90, 81);
            lblDataId.Name = "lblDataId";
            lblDataId.Size = new Size(12, 15);
            lblDataId.TabIndex = 18;
            lblDataId.Text = "-";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(120, 81);
            label5.Name = "label5";
            label5.Size = new Size(91, 15);
            label5.TabIndex = 16;
            label5.Text = "最終受信時刻：";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 81);
            label4.Name = "label4";
            label4.Size = new Size(57, 15);
            label4.TabIndex = 15;
            label4.Text = "Data ID：";
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSize = true;
            lblDeviceName.Location = new Point(246, 55);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(50, 15);
            lblDeviceName.TabIndex = 14;
            lblDeviceName.Text = "SSE-GL1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 55);
            label2.Name = "label2";
            label2.Size = new Size(67, 15);
            label2.TabIndex = 13;
            label2.Text = "接続状態：";
            // 
            // btnStopNotify
            // 
            btnStopNotify.Location = new Point(168, 22);
            btnStopNotify.Name = "btnStopNotify";
            btnStopNotify.Size = new Size(75, 23);
            btnStopNotify.TabIndex = 12;
            btnStopNotify.Text = "Notify停止";
            btnStopNotify.UseVisualStyleBackColor = true;
            btnStopNotify.Click += btnStopNotify_Click;
            // 
            // btnStartNotify
            // 
            btnStartNotify.Location = new Point(87, 22);
            btnStartNotify.Name = "btnStartNotify";
            btnStartNotify.Size = new Size(75, 23);
            btnStartNotify.TabIndex = 11;
            btnStartNotify.Text = "Notify開始";
            btnStartNotify.UseVisualStyleBackColor = true;
            btnStartNotify.Click += btnStartNotify_Click;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(6, 22);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 1;
            btnSearch.Text = "BLE接続";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // grpAcceleration
            // 
            grpAcceleration.Controls.Add(lblPrimaryZ);
            grpAcceleration.Controls.Add(lblPrimaryY);
            grpAcceleration.Controls.Add(lblPrimaryX);
            grpAcceleration.Controls.Add(label8);
            grpAcceleration.Controls.Add(label7);
            grpAcceleration.Controls.Add(label1);
            grpAcceleration.Location = new Point(21, 138);
            grpAcceleration.Name = "grpAcceleration";
            grpAcceleration.Size = new Size(393, 64);
            grpAcceleration.TabIndex = 20;
            grpAcceleration.TabStop = false;
            grpAcceleration.Text = "加速度";
            // 
            // lblPrimaryZ
            // 
            lblPrimaryZ.AutoSize = true;
            lblPrimaryZ.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblPrimaryZ.Location = new Point(261, 28);
            lblPrimaryZ.Name = "lblPrimaryZ";
            lblPrimaryZ.Size = new Size(16, 21);
            lblPrimaryZ.TabIndex = 16;
            lblPrimaryZ.Text = "-";
            lblPrimaryZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPrimaryY
            // 
            lblPrimaryY.AutoSize = true;
            lblPrimaryY.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblPrimaryY.Location = new Point(145, 28);
            lblPrimaryY.Name = "lblPrimaryY";
            lblPrimaryY.Size = new Size(16, 21);
            lblPrimaryY.TabIndex = 15;
            lblPrimaryY.Text = "-";
            lblPrimaryY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPrimaryX
            // 
            lblPrimaryX.AutoSize = true;
            lblPrimaryX.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblPrimaryX.Location = new Point(33, 28);
            lblPrimaryX.Name = "lblPrimaryX";
            lblPrimaryX.Size = new Size(16, 21);
            lblPrimaryX.TabIndex = 14;
            lblPrimaryX.Text = "-";
            lblPrimaryX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(229, 31);
            label8.Name = "label8";
            label8.Size = new Size(14, 15);
            label8.TabIndex = 2;
            label8.Text = "Z";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(119, 31);
            label7.Name = "label7";
            label7.Size = new Size(14, 15);
            label7.TabIndex = 1;
            label7.Text = "Y";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 31);
            label1.Name = "label1";
            label1.Size = new Size(14, 15);
            label1.TabIndex = 0;
            label1.Text = "X";
            // 
            // grpGyroscope
            // 
            grpGyroscope.Controls.Add(label19);
            grpGyroscope.Controls.Add(label20);
            grpGyroscope.Controls.Add(label18);
            grpGyroscope.Controls.Add(lblFusedAngleZ);
            grpGyroscope.Controls.Add(label16);
            grpGyroscope.Controls.Add(label17);
            grpGyroscope.Controls.Add(lblFusedAngleY);
            grpGyroscope.Controls.Add(lblFusedAngleX);
            grpGyroscope.Controls.Add(label15);
            grpGyroscope.Controls.Add(lblAccelAngleY);
            grpGyroscope.Controls.Add(lblAccelAngleX);
            grpGyroscope.Controls.Add(lblAccelAngleTitle);
            grpGyroscope.Controls.Add(label12);
            grpGyroscope.Controls.Add(label13);
            grpGyroscope.Controls.Add(label14);
            grpGyroscope.Controls.Add(lblAngleZ);
            grpGyroscope.Controls.Add(lblAngleY);
            grpGyroscope.Controls.Add(lblAngleX);
            grpGyroscope.Controls.Add(label9);
            grpGyroscope.Controls.Add(label10);
            grpGyroscope.Controls.Add(label11);
            grpGyroscope.Controls.Add(lblSecondaryZ);
            grpGyroscope.Controls.Add(lblSecondaryY);
            grpGyroscope.Controls.Add(lblSecondaryX);
            grpGyroscope.Location = new Point(20, 208);
            grpGyroscope.Name = "grpGyroscope";
            grpGyroscope.Size = new Size(394, 176);
            grpGyroscope.TabIndex = 21;
            grpGyroscope.TabStop = false;
            grpGyroscope.Text = "ジャイロ";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(189, 102);
            label19.Name = "label19";
            label19.Size = new Size(14, 15);
            label19.TabIndex = 42;
            label19.Text = "Y";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(84, 102);
            label20.Name = "label20";
            label20.Size = new Size(14, 15);
            label20.TabIndex = 41;
            label20.Text = "X";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(294, 138);
            label18.Name = "label18";
            label18.Size = new Size(14, 15);
            label18.TabIndex = 40;
            label18.Text = "Z";
            // 
            // lblFusedAngleZ
            // 
            lblFusedAngleZ.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblFusedAngleZ.Location = new Point(313, 130);
            lblFusedAngleZ.Name = "lblFusedAngleZ";
            lblFusedAngleZ.Size = new Size(65, 30);
            lblFusedAngleZ.TabIndex = 39;
            lblFusedAngleZ.Text = "-";
            lblFusedAngleZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(189, 138);
            label16.Name = "label16";
            label16.Size = new Size(14, 15);
            label16.TabIndex = 38;
            label16.Text = "Y";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(84, 138);
            label17.Name = "label17";
            label17.Size = new Size(14, 15);
            label17.TabIndex = 37;
            label17.Text = "X";
            // 
            // lblFusedAngleY
            // 
            lblFusedAngleY.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblFusedAngleY.Location = new Point(209, 130);
            lblFusedAngleY.Name = "lblFusedAngleY";
            lblFusedAngleY.Size = new Size(65, 30);
            lblFusedAngleY.TabIndex = 36;
            lblFusedAngleY.Text = "-";
            lblFusedAngleY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblFusedAngleX
            // 
            lblFusedAngleX.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblFusedAngleX.Location = new Point(104, 130);
            lblFusedAngleX.Name = "lblFusedAngleX";
            lblFusedAngleX.Size = new Size(65, 30);
            lblFusedAngleX.TabIndex = 35;
            lblFusedAngleX.Text = "-";
            lblFusedAngleX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(15, 138);
            label15.Name = "label15";
            label15.Size = new Size(43, 15);
            label15.TabIndex = 34;
            label15.Text = "姿勢角";
            // 
            // lblAccelAngleY
            // 
            lblAccelAngleY.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold);
            lblAccelAngleY.Location = new Point(209, 94);
            lblAccelAngleY.Name = "lblAccelAngleY";
            lblAccelAngleY.Size = new Size(65, 30);
            lblAccelAngleY.TabIndex = 33;
            lblAccelAngleY.Text = "-";
            lblAccelAngleY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblAccelAngleX
            // 
            lblAccelAngleX.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold);
            lblAccelAngleX.Location = new Point(104, 94);
            lblAccelAngleX.Name = "lblAccelAngleX";
            lblAccelAngleX.Size = new Size(65, 30);
            lblAccelAngleX.TabIndex = 32;
            lblAccelAngleX.Text = "-";
            lblAccelAngleX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblAccelAngleTitle
            // 
            lblAccelAngleTitle.AutoSize = true;
            lblAccelAngleTitle.Location = new Point(14, 102);
            lblAccelAngleTitle.Name = "lblAccelAngleTitle";
            lblAccelAngleTitle.Size = new Size(55, 15);
            lblAccelAngleTitle.TabIndex = 31;
            lblAccelAngleTitle.Text = "加速度角";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(230, 72);
            label12.Name = "label12";
            label12.Size = new Size(14, 15);
            label12.TabIndex = 30;
            label12.Text = "Z";
            label12.Visible = false;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(120, 72);
            label13.Name = "label13";
            label13.Size = new Size(14, 15);
            label13.TabIndex = 29;
            label13.Text = "Y";
            label13.Visible = false;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(15, 72);
            label14.Name = "label14";
            label14.Size = new Size(14, 15);
            label14.TabIndex = 28;
            label14.Text = "X";
            label14.Visible = false;
            // 
            // lblAngleZ
            // 
            lblAngleZ.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblAngleZ.Location = new Point(262, 64);
            lblAngleZ.Name = "lblAngleZ";
            lblAngleZ.Size = new Size(80, 30);
            lblAngleZ.TabIndex = 27;
            lblAngleZ.Text = "-";
            lblAngleZ.TextAlign = ContentAlignment.MiddleRight;
            lblAngleZ.Visible = false;
            // 
            // lblAngleY
            // 
            lblAngleY.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblAngleY.Location = new Point(146, 64);
            lblAngleY.Name = "lblAngleY";
            lblAngleY.Size = new Size(80, 30);
            lblAngleY.TabIndex = 26;
            lblAngleY.Text = "-";
            lblAngleY.TextAlign = ContentAlignment.MiddleRight;
            lblAngleY.Visible = false;
            // 
            // lblAngleX
            // 
            lblAngleX.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblAngleX.Location = new Point(35, 64);
            lblAngleX.Name = "lblAngleX";
            lblAngleX.Size = new Size(80, 30);
            lblAngleX.TabIndex = 25;
            lblAngleX.Text = "-";
            lblAngleX.TextAlign = ContentAlignment.MiddleRight;
            lblAngleX.Visible = false;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(230, 35);
            label9.Name = "label9";
            label9.Size = new Size(14, 15);
            label9.TabIndex = 24;
            label9.Text = "Z";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(120, 35);
            label10.Name = "label10";
            label10.Size = new Size(14, 15);
            label10.TabIndex = 23;
            label10.Text = "Y";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(15, 35);
            label11.Name = "label11";
            label11.Size = new Size(14, 15);
            label11.TabIndex = 22;
            label11.Text = "X";
            // 
            // lblSecondaryZ
            // 
            lblSecondaryZ.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblSecondaryZ.Location = new Point(262, 27);
            lblSecondaryZ.Name = "lblSecondaryZ";
            lblSecondaryZ.Size = new Size(80, 30);
            lblSecondaryZ.TabIndex = 21;
            lblSecondaryZ.Text = "-";
            lblSecondaryZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSecondaryY
            // 
            lblSecondaryY.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblSecondaryY.Location = new Point(146, 27);
            lblSecondaryY.Name = "lblSecondaryY";
            lblSecondaryY.Size = new Size(80, 30);
            lblSecondaryY.TabIndex = 20;
            lblSecondaryY.Text = "-";
            lblSecondaryY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSecondaryX
            // 
            lblSecondaryX.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblSecondaryX.Location = new Point(35, 27);
            lblSecondaryX.Name = "lblSecondaryX";
            lblSecondaryX.Size = new Size(80, 30);
            lblSecondaryX.TabIndex = 19;
            lblSecondaryX.Text = "-";
            lblSecondaryX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // pnlMotionGraph
            // 
            pnlMotionGraph.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlMotionGraph.BorderStyle = BorderStyle.FixedSingle;
            pnlMotionGraph.Controls.Add(lblGraphPlaceholder);
            pnlMotionGraph.Location = new Point(444, 150);
            pnlMotionGraph.Name = "pnlMotionGraph";
            pnlMotionGraph.Size = new Size(625, 150);
            pnlMotionGraph.TabIndex = 22;
            pnlMotionGraph.Paint += pnlMotionGraph_Paint;
            // 
            // lblGraphPlaceholder
            // 
            lblGraphPlaceholder.AutoSize = true;
            lblGraphPlaceholder.Location = new Point(21, 7);
            lblGraphPlaceholder.Name = "lblGraphPlaceholder";
            lblGraphPlaceholder.Size = new Size(85, 15);
            lblGraphPlaceholder.TabIndex = 0;
            lblGraphPlaceholder.Text = "リアルタイム波形";
            // 
            // grpDebugLog
            // 
            grpDebugLog.Controls.Add(txtReceive);
            grpDebugLog.Location = new Point(21, 625);
            grpDebugLog.Name = "grpDebugLog";
            grpDebugLog.Size = new Size(1051, 124);
            grpDebugLog.TabIndex = 23;
            grpDebugLog.TabStop = false;
            grpDebugLog.Text = "デバッグログ";
            // 
            // btnRfcommTest
            // 
            btnRfcommTest.Location = new Point(20, 404);
            btnRfcommTest.Name = "btnRfcommTest";
            btnRfcommTest.Size = new Size(128, 23);
            btnRfcommTest.TabIndex = 24;
            btnRfcommTest.Text = "RFCOMM確認";
            btnRfcommTest.UseVisualStyleBackColor = true;
            btnRfcommTest.Click += btnRfcommTest_Click;
            // 
            // pnlGyroGraph
            // 
            pnlGyroGraph.BorderStyle = BorderStyle.FixedSingle;
            pnlGyroGraph.Location = new Point(444, 310);
            pnlGyroGraph.Name = "pnlGyroGraph";
            pnlGyroGraph.Size = new Size(625, 150);
            pnlGyroGraph.TabIndex = 25;
            pnlGyroGraph.Paint += pnlGyroGraph_Paint;
            // 
            // btnGyroBias
            // 
            btnGyroBias.Location = new Point(20, 444);
            btnGyroBias.Name = "btnGyroBias";
            btnGyroBias.Size = new Size(128, 23);
            btnGyroBias.TabIndex = 26;
            btnGyroBias.Text = "ジャイロ零点測定";
            btnGyroBias.UseVisualStyleBackColor = true;
            btnGyroBias.Click += btnGyroBias_Click;
            // 
            // btnGyroAngleStart
            // 
            btnGyroAngleStart.Location = new Point(21, 484);
            btnGyroAngleStart.Name = "btnGyroAngleStart";
            btnGyroAngleStart.Size = new Size(127, 23);
            btnGyroAngleStart.TabIndex = 27;
            btnGyroAngleStart.Text = "角度測定開始\n";
            btnGyroAngleStart.UseVisualStyleBackColor = true;
            btnGyroAngleStart.Visible = false;
            btnGyroAngleStart.Click += btnGyroAngleStart_Click;
            // 
            // btnGyroAngleStop
            // 
            btnGyroAngleStop.Location = new Point(23, 524);
            btnGyroAngleStop.Name = "btnGyroAngleStop";
            btnGyroAngleStop.Size = new Size(123, 23);
            btnGyroAngleStop.TabIndex = 28;
            btnGyroAngleStop.Text = "角度測定停止";
            btnGyroAngleStop.UseVisualStyleBackColor = true;
            btnGyroAngleStop.Visible = false;
            btnGyroAngleStop.Click += btnGyroAngleStop_Click;
            // 
            // btnAngleReset
            // 
            btnAngleReset.Location = new Point(21, 564);
            btnAngleReset.Name = "btnAngleReset";
            btnAngleReset.Size = new Size(127, 23);
            btnAngleReset.TabIndex = 29;
            btnAngleReset.Text = "アドレス基準";
            btnAngleReset.UseVisualStyleBackColor = true;
            btnAngleReset.Click += btnAngleReset_Click;
            // 
            // pnlAngleGraph
            // 
            pnlAngleGraph.BorderStyle = BorderStyle.FixedSingle;
            pnlAngleGraph.Location = new Point(444, 470);
            pnlAngleGraph.Name = "pnlAngleGraph";
            pnlAngleGraph.Size = new Size(625, 150);
            pnlAngleGraph.TabIndex = 26;
            pnlAngleGraph.Paint += pnlAngleGraph_Paint;
            // 
            // btnRfcommConnectTest
            // 
            btnRfcommConnectTest.Location = new Point(188, 404);
            btnRfcommConnectTest.Name = "btnRfcommConnectTest";
            btnRfcommConnectTest.Size = new Size(126, 23);
            btnRfcommConnectTest.TabIndex = 30;
            btnRfcommConnectTest.Text = "RFCOMM接続確認";
            btnRfcommConnectTest.UseVisualStyleBackColor = true;
            btnRfcommConnectTest.Click += btnRfcommConnectTest_Click;
            // 
            // btnRfcommReceiveTest
            // 
            btnRfcommReceiveTest.Location = new Point(188, 444);
            btnRfcommReceiveTest.Name = "btnRfcommReceiveTest";
            btnRfcommReceiveTest.Size = new Size(126, 23);
            btnRfcommReceiveTest.TabIndex = 31;
            btnRfcommReceiveTest.Text = "RFCOMM受信確認\n";
            btnRfcommReceiveTest.UseVisualStyleBackColor = true;
            btnRfcommReceiveTest.Click += btnRfcommReceiveTest_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 761);
            Controls.Add(btnRfcommReceiveTest);
            Controls.Add(btnRfcommConnectTest);
            Controls.Add(pnlAngleGraph);
            Controls.Add(btnAngleReset);
            Controls.Add(btnGyroAngleStop);
            Controls.Add(btnGyroAngleStart);
            Controls.Add(btnGyroBias);
            Controls.Add(pnlGyroGraph);
            Controls.Add(btnRfcommTest);
            Controls.Add(grpDebugLog);
            Controls.Add(pnlMotionGraph);
            Controls.Add(grpGyroscope);
            Controls.Add(grpAcceleration);
            Controls.Add(grpConnection);
            MinimumSize = new Size(1000, 700);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SSE-GL1 Viewer";
            Load += MainForm_Load;
            grpConnection.ResumeLayout(false);
            grpConnection.PerformLayout();
            grpAcceleration.ResumeLayout(false);
            grpAcceleration.PerformLayout();
            grpGyroscope.ResumeLayout(false);
            grpGyroscope.PerformLayout();
            pnlMotionGraph.ResumeLayout(false);
            pnlMotionGraph.PerformLayout();
            grpDebugLog.ResumeLayout(false);
            grpDebugLog.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TextBox txtReceive;
        private GroupBox grpConnection;
        private Button btnStopNotify;
        private Button btnStartNotify;
        private Button btnSearch;
        private Label label5;
        private Label label4;
        private Label lblDeviceName;
        private Label label2;
        private Label lblTimestamp;
        private Label lblDataId;
        private Label lblConnectionStatus;
        private Label label3;
        private Label label6;
        private ListBox lstDevices;
        private GroupBox grpAcceleration;
        private Label lblPrimaryZ;
        private Label lblPrimaryY;
        private Label lblPrimaryX;
        private Label label8;
        private Label label7;
        private Label label1;
        private GroupBox grpGyroscope;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label lblSecondaryZ;
        private Label lblSecondaryY;
        private Label lblSecondaryX;
        private DoubleBufferedPanel pnlMotionGraph;
        private Label lblGraphPlaceholder;
        private GroupBox grpDebugLog;
        private Button btnRfcommTest;
        private DoubleBufferedPanel pnlGyroGraph;
        private Button btnGyroBias;
        private Button btnGyroAngleStart;
        private Button btnGyroAngleStop;
        private Label label12;
        private Label label13;
        private Label label14;
        private Label lblAngleZ;
        private Label lblAngleY;
        private Label lblAngleX;
        private Button btnAngleReset;
        private Label lblAccelAngleX;
        private Label lblAccelAngleTitle;
        private Label lblAccelAngleY;
        private Label label16;
        private Label label17;
        private Label lblFusedAngleY;
        private Label lblFusedAngleX;
        private Label label15;
        private DoubleBufferedPanel pnlAngleGraph;
        private Label label18;
        private Label lblFusedAngleZ;
        private Button btnRfcommConnectTest;
        private Button btnRfcommReceiveTest;
        private Label label19;
        private Label label20;
    }
}
