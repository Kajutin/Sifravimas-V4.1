using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Sifravimas_V4._1
{
    public partial class Login : Form
    {
        bool shouldIRun = false;
        public Login()
        {
            InitializeComponent();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (shouldIRun == false)
            {
                Application.Exit();
                base.OnFormClosing(e);

            }    
        }
        private void LoginButton_Click(object sender, EventArgs e)
        {
            shouldIRun = true;
            if(UsernameTextBox.Text == "admin" && PasswordTextBox.Text == "admin")
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                PasswordTextBox.Text = "";
                UsernameTextBox.Text = "";
                MessageBox.Show("Incorrect credentials, please try again.");
            }
        }
    }
}
