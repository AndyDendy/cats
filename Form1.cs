using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Myronov
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        TCP tcp;
        HTTP http;
        private void Form1_Load(object sender, EventArgs e)
        {
            string arr = File.ReadAllText("manifest.xml");
            textBox1.Text = arr;
        }
        void Manifest()
        {
            File.WriteAllText("manifest.xml",textBox1.Text);
            
        }
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manifest();
            tcp = new TCP();
            tcp.Start();
            http = new HTTP();
            http.Start();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcp.Stop();
            http.Stop();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopToolStripMenuItem_Click( sender,  e);
            startToolStripMenuItem_Click(sender, e);
        }
    }
}
