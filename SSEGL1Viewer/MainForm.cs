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
        }
    }
}
