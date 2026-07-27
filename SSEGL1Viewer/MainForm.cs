using System;
using System.Windows.Forms;
using SSEGL1Viewer.Bluetooth;

namespace SSEGL1Viewer
{
    public partial class MainForm : Form
    {
        private BTManager manager = new BTManager();
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            lstDevices.Items.Clear();

            BTManager manager = new BTManager();

            var devices = await manager.ScanAsync();

            foreach (var d in devices)
            {
                lstDevices.Items.Add(d);
            }
        }
    }
}
