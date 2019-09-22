using System;
using System.Windows.Forms;

namespace SCTV
{
    public partial class StartInstructions : Form
    {
        public StartInstructions()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
