
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Guitar_Tuner
{
    public partial class Form1 : Form
    {
        public Sound sound;
        bool formLoading;
        public Form1(Sound s)
        {
            InitializeComponent();
            sound = s;
        }

        // метод для обновления текста из другого потока
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

        private void Form1_Load(object sender, EventArgs e)
        {
            formLoading = true;

            dataGridView1.Rows.Clear(); // на всякий случай очистим

            foreach (var note in sound.noteBaseFreqs)
            {
                for (int octave = 0; octave <= 4; octave++) // до 4-й октавы включительно
                {
                    float freq = note.Value * (float)Math.Pow(2, octave);
                    if (freq < 20 || freq > 5000) continue; // фильтр слишком низких/высоких частот

                    dataGridView1.Rows.Add(
                        note.Key + octave, // Нота
                        freq,              // Частота
                        ""                 // клавиша или мышь (пустая по умолчанию)
                    );
                }
            }

            formLoading = false;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (formLoading) return;
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;

            var row = dataGridView1.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            string note = row.Cells[0].Value?.ToString();
            string key = row.Cells[2].Value?.ToString();

            if (!string.IsNullOrEmpty(note))
            {
                sound.NoteToKey[note] = key;
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
    }
}
