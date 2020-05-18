using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClinetForm
{
    public partial class Client: UserControl
    {
        public Client()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var id = tbUID.Text;
            var pw = tbPW.Text;
            tbResult.Text = $"id={id},pw={pw}";
        }
    }
}
