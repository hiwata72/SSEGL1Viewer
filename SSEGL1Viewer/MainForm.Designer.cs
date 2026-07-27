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
            btnSearch = new Button();
            lstDevices = new ListBox();
            SuspendLayout();
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(50, 48);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 0;
            btnSearch.Text = "BLE接続";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // lstDevices
            // 
            lstDevices.FormattingEnabled = true;
            lstDevices.Location = new Point(50, 93);
            lstDevices.Name = "lstDevices";
            lstDevices.Size = new Size(120, 94);
            lstDevices.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 661);
            Controls.Add(lstDevices);
            Controls.Add(btnSearch);
            Name = "MainForm";
            Text = "SSE-GL1 Viewer Ver0.1";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button btnSearch;
        private ListBox lstDevices;
    }
}
