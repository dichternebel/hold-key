using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace HoldKey
{
    public partial class Form1 : Form
    {
        private ProgramViewModel ViewModel { get; set; }

        public Form1(ProgramViewModel viewModel)
        {
            this.ViewModel = viewModel;
            InitializeComponent();

            this.Text = this.ViewModel.WindowTitle;
            this.button1.DataBindings.Add("BackgroundImage", this.ViewModel, "ButtonImage");
        }

        private void Form1_Load(object sender, EventArgs eventArgs)
        {
            foreach (Button b in this.Controls.OfType<Button>())
            {
                b.MouseEnter += (s, e) => b.Cursor = Cursors.Hand;
                b.MouseLeave += (s, e) => b.Cursor = Cursors.Arrow;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.ShowInTaskbar = this.WindowState != FormWindowState.Minimized;
            this.notifyIcon1.Visible = !this.ShowInTaskbar;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.github.com/dichternebel/hold-key");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.ViewModel.IsSoundEnabled = !this.ViewModel.IsSoundEnabled;
        }
    }
}
