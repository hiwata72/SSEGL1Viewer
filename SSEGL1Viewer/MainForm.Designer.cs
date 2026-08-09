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
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            lblSecondaryZ = new Label();
            lblSecondaryY = new Label();
            lblSecondaryX = new Label();
            pnlMotionGraph = new DoubleBufferedPanel();
            lblGraphPlaceholder = new Label();
            grpDebugLog = new GroupBox();
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
            lstDevices.Location = new Point(383, 40);
            lstDevices.Name = "lstDevices";
            lstDevices.Size = new Size(649, 64);
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
            grpAcceleration.Size = new Size(359, 64);
            grpAcceleration.TabIndex = 20;
            grpAcceleration.TabStop = false;
            grpAcceleration.Text = "加速度";
            // 
            // lblPrimaryZ
            // 
            lblPrimaryZ.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold);
            lblPrimaryZ.Location = new Point(261, 22);
            lblPrimaryZ.Name = "lblPrimaryZ";
            lblPrimaryZ.Size = new Size(80, 30);
            lblPrimaryZ.TabIndex = 16;
            lblPrimaryZ.Text = "-";
            lblPrimaryZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPrimaryY
            // 
            lblPrimaryY.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold);
            lblPrimaryY.Location = new Point(145, 22);
            lblPrimaryY.Name = "lblPrimaryY";
            lblPrimaryY.Size = new Size(80, 30);
            lblPrimaryY.TabIndex = 15;
            lblPrimaryY.Text = "-";
            lblPrimaryY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPrimaryX
            // 
            lblPrimaryX.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 128);
            lblPrimaryX.Location = new Point(33, 22);
            lblPrimaryX.Name = "lblPrimaryX";
            lblPrimaryX.Size = new Size(80, 30);
            lblPrimaryX.TabIndex = 14;
            lblPrimaryX.Text = "-";
            lblPrimaryX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(229, 34);
            label8.Name = "label8";
            label8.Size = new Size(14, 15);
            label8.TabIndex = 2;
            label8.Text = "Z";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(119, 34);
            label7.Name = "label7";
            label7.Size = new Size(14, 15);
            label7.TabIndex = 1;
            label7.Text = "Y";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 34);
            label1.Name = "label1";
            label1.Size = new Size(14, 15);
            label1.TabIndex = 0;
            label1.Text = "X";
            // 
            // grpGyroscope
            // 
            grpGyroscope.Controls.Add(label9);
            grpGyroscope.Controls.Add(label10);
            grpGyroscope.Controls.Add(label11);
            grpGyroscope.Controls.Add(lblSecondaryZ);
            grpGyroscope.Controls.Add(lblSecondaryY);
            grpGyroscope.Controls.Add(lblSecondaryX);
            grpGyroscope.Location = new Point(20, 208);
            grpGyroscope.Name = "grpGyroscope";
            grpGyroscope.Size = new Size(360, 67);
            grpGyroscope.TabIndex = 21;
            grpGyroscope.TabStop = false;
            grpGyroscope.Text = "ジャイロ";
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
            lblSecondaryZ.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold);
            lblSecondaryZ.Location = new Point(262, 27);
            lblSecondaryZ.Name = "lblSecondaryZ";
            lblSecondaryZ.Size = new Size(80, 30);
            lblSecondaryZ.TabIndex = 21;
            lblSecondaryZ.Text = "-";
            lblSecondaryZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSecondaryY
            // 
            lblSecondaryY.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold);
            lblSecondaryY.Location = new Point(146, 27);
            lblSecondaryY.Name = "lblSecondaryY";
            lblSecondaryY.Size = new Size(80, 30);
            lblSecondaryY.TabIndex = 20;
            lblSecondaryY.Text = "-";
            lblSecondaryY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSecondaryX
            // 
            lblSecondaryX.Font = new Font("Yu Gothic UI", 15.75F, FontStyle.Bold);
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
            pnlMotionGraph.Location = new Point(403, 148);
            pnlMotionGraph.Name = "pnlMotionGraph";
            pnlMotionGraph.Size = new Size(669, 471);
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 761);
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
    }
}
