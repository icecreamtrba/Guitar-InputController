
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Guitar_Tuner
{

    public partial class Form1 : Form
    {
        OverlayGUI overlayGUI;
        public bool isOverlayShown;
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
                if(isOverlayShown) 
                    overlayGUI.UpdateNoteAndFreq(note, freq);
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

        private void button1_Click(object sender, EventArgs e)
        {
            overlayGUI = new OverlayGUI(sound, this);
            overlayGUI.Show();
            isOverlayShown = true;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                var list = new List<RowData>();

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        list.Add(new RowData
                        {
                            Note = row.Cells[0]?.Value?.ToString() ?? string.Empty,
                            Freq = row.Cells[1]?.Value?.ToString() ?? string.Empty,
                            Key = row.Cells[2]?.Value?.ToString() ?? string.Empty
                        });
                    }
                }

                if (list.Count == 0)
                {
                    MessageBox.Show("Нет данных для сохранения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string json = JsonConvert.SerializeObject(list, Newtonsoft.Json.Formatting.Indented);

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "JSON files|*.json";
                    sfd.DefaultExt = "json";
                    if (sfd.ShowDialog() != DialogResult.OK) return;

                    File.WriteAllText(sfd.FileName, json);
                }

                MessageBox.Show("Сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string json = null;

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "JSON files|*.json";
                    if (ofd.ShowDialog() != DialogResult.OK) return;

                    json = File.ReadAllText(ofd.FileName);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    MessageBox.Show("Файл пустой.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var list = JsonConvert.DeserializeObject<List<RowData>>(json);
                if (list == null)
                {
                    MessageBox.Show("Не удалось прочитать данные из файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dataGridView1.Rows.Clear();
                foreach (var item in list)
                {
                    dataGridView1.Rows.Add(item.Note ?? string.Empty, item.Freq ?? string.Empty, item.Key ?? string.Empty);
                }

                MessageBox.Show("Загружено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (JsonException jex)
            {
                MessageBox.Show($"Ошибка парсинга JSON:\n{jex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
    public class RowData
    {
        public string Note { get; set; }
        public string Freq { get; set; }
        public string Key { get; set; }
    }
}
