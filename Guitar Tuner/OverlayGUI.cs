using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Guitar_Tuner
{
    public partial class OverlayGUI : Form
    {
        public Sound sound;
        public Form1 parent;
        public OverlayGUI(Sound s, Form1 p)
        {
            InitializeComponent();
            sound = s;
            parent = p;
        }
        public void UpdateNoteAndFreq(string note, string freq)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateNoteAndFreq(note, freq)));
            }
            else
            {
                label2.Text = note;
                label5.Text = freq;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int value))
            {
                MouseSimulator.mouseSpeed = value;
                textBox1.BackColor = Color.White;
            }
            else
            {
                textBox1.BackColor = Color.Red;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            parent.isOverlayShown = false;
            this.Close();
        }
    }
}
