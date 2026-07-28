using System;
using System.Windows.Forms;
using SSEGL1Viewer.Bluetooth;

namespace SSEGL1Viewer
{
    public partial class MainForm : Form
    {
        private readonly BTManager _btManager = new();
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        //private async void btnSearch_Click(object sender, EventArgs e)
        //{
        //    btnSearch.Enabled = false;
        //    lstDevices.Items.Clear();

        //    try
        //    {
        //        List<BluetoothDeviceInfo> devices =
        //            await _btManager.ScanAsync();

        //        foreach (BluetoothDeviceInfo device in devices)
        //        {
        //            lstDevices.Items.Add(device);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(
        //            ex.Message,
        //            "BLE検索エラー",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error);
        //    }
        //    finally
        //    {
        //        btnSearch.Enabled = true;
        //    }
        //}

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            btnSearch.Enabled = false;

            try
            {
                string result = await _btManager.TestRfcommServicesAsync();

                MessageBox.Show(
                    result,
                    "SSE-GL1 RFCOMMサービス確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "RFCOMMエラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSearch.Enabled = true;
            }
        }
    }
}
