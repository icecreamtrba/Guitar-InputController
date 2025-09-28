
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Guitar_Tuner
{
    public partial class Form1 : Form
    {

        MinecraftMod minecraftMod;


        OverlayGUI overlayGUI;
        public bool isOverlayShown;
        public Sound sound;
        bool formLoading;
        public Form1()
        {
            InitializeComponent();
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

        private async void Form1_Load(object sender, EventArgs e)
        {
            formLoading = true;

            try
            {
                minecraftMod = new MinecraftMod("127.0.0.1", 5000);
                await minecraftMod.ConnectAsync();

                // ★★★★ ДОБАВЬТЕ ПРОВЕРКУ ПЕРЕД СОЗДАНИЕМ SOUND ★★★★
                Console.WriteLine($"[Form1] Before creating Sound - minecraftMod: {minecraftMod != null}");
                Console.WriteLine($"[Form1] Before creating Sound - IsConnected: {minecraftMod?.IsConnected}");

                sound = new Sound(minecraftMod); // Передаем mod в Sound

                Console.WriteLine($"[Form1] After creating Sound - sound: {sound != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Form1] Exception: {ex.Message}");
                MessageBox.Show("Не удалось подключиться к Minecraft модулю:\n" + ex.Message);

                // ★★★★ ПРОВЕРЬТЕ СОСТОЯНИЕ MINECRAFTMOD ПОСЛЕ ОШИБКИ ★★★★
                Console.WriteLine($"[Form1] After exception - minecraftMod: {minecraftMod != null}");
                if (minecraftMod != null)
                {
                    Console.WriteLine($"[Form1] After exception - IsConnected: {minecraftMod.IsConnected}");
                }

                sound = new Sound(minecraftMod); // ★★★★ ВСЕ РАВНО ПЕРЕДАЕМ MOD ★★★★
            }

            sound.gui = this;

            // ★★★★ ПОДПИСКА ПОСЛЕ СОЗДАНИЯ SOUND ★★★★
            btnWindows.Click += (s, ev) => sound.SwitchToWindowsMode();
            btnMinecraft.Click += (s, ev) => sound.SwitchToMinecraftMode();

            dataGridView1.Rows.Clear();

            foreach (var note in sound.noteBaseFreqs)
            {
                for (int octave = 0; octave <= 4; octave++)
                {
                    float freq = note.Value * (float)Math.Pow(2, octave);
                    if (freq < 20 || freq > 5000) continue;

                    dataGridView1.Rows.Add(
                        note.Key + octave,
                        freq,
                        ""
                    );
                    dataGridView2.Rows.Add(
                        note.Key + octave,
                        freq,
                        ""
                    );
                }
            }

            formLoading = false;

            // ★★★★ ОБНОВИТЕ ОТОБРАЖЕНИЕ РЕЖИМА ПОСЛЕ ЗАГРУЗКИ ★★★★
            UpdateControlModeDisplay();
        }
        public void UpdateControlModeDisplay()
        {
            if (sound?.CurrentControlManager is MinecraftControlManager)
            {
                toolStripStatusLabel2.Text = "Режим: Minecraft";
                toolStripStatusLabel2.ForeColor = Color.Green;
            }
            else
            {
                toolStripStatusLabel2.Text = "Режим: Windows";
                toolStripStatusLabel2.ForeColor = Color.Blue;
            }
        }
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (formLoading) return;
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;

            var row = dataGridView1.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            string note = row.Cells[0].Value?.ToString();
            string key = row.Cells[2].Value?.ToString();

            Console.WriteLine($"[Form1] Windows mapping: {note} -> {key}");

            if (!string.IsNullOrEmpty(note))
            {
                // ★★★★ СОХРАНЯЕМ В СЛОВАРЬ WINDOWS ★★★★
                sound.NoteToKeyWindows[note] = key;
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
                var windowsList = new List<RowData>();
                var minecraftList = new List<RowData>();

                // ★★★★ СОХРАНЯЕМ ОБА РЕЖИМА ★★★★
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        windowsList.Add(new RowData
                        {
                            Note = row.Cells[0]?.Value?.ToString() ?? string.Empty,
                            Freq = row.Cells[1]?.Value?.ToString() ?? string.Empty,
                            Key = row.Cells[2]?.Value?.ToString() ?? string.Empty
                        });
                    }
                }

                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        minecraftList.Add(new RowData
                        {
                            Note = row.Cells[0]?.Value?.ToString() ?? string.Empty,
                            Freq = row.Cells[1]?.Value?.ToString() ?? string.Empty,
                            Key = row.Cells[2]?.Value?.ToString() ?? string.Empty
                        });
                    }
                }

                var saveData = new
                {
                    WindowsMappings = windowsList,
                    MinecraftMappings = minecraftList
                };

                string json = JsonConvert.SerializeObject(saveData, Newtonsoft.Json.Formatting.Indented);

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "JSON files|*.json";
                    sfd.DefaultExt = "json";
                    if (sfd.ShowDialog() != DialogResult.OK) return;

                    File.WriteAllText(sfd.FileName, json);
                }

                MessageBox.Show("Оба режима сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                var data = JsonConvert.DeserializeObject<dynamic>(json);

                // ★★★★ ЗАГРУЖАЕМ ОБА РЕЖИМА ★★★★
                if (data?.WindowsMappings != null)
                {
                    dataGridView1.Rows.Clear();
                    foreach (var item in data.WindowsMappings)
                    {
                        dataGridView1.Rows.Add(item.Note ?? string.Empty, item.Freq ?? string.Empty, item.Key ?? string.Empty);
                    }
                }

                if (data?.MinecraftMappings != null)
                {
                    dataGridView2.Rows.Clear();
                    foreach (var item in data.MinecraftMappings)
                    {
                        dataGridView2.Rows.Add(item.Note ?? string.Empty, item.Freq ?? string.Empty, item.Key ?? string.Empty);
                    }
                }

                MessageBox.Show("Оба режима загружены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (minecraftMod == null)
                {
                    minecraftMod = new MinecraftMod("127.0.0.1", 5000);
                }

                await minecraftMod.ConnectAsync();

                // ★★★★ ОБНОВИТЕ SOUND С НОВЫМ ПОДКЛЮЧЕНИЕМ ★★★★
                if (sound != null)
                {
                    sound.UpdateMinecraftMod(minecraftMod);
                }

                MessageBox.Show("✅ Успешно подключено к Minecraft!", "Подключение");
                UpdateControlModeDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Не удалось подключиться: {ex.Message}", "Ошибка");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            minecraftMod?.Disconnect();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                int device = sound.SelectInputDevice();
                MessageBox.Show($"Выбрано устройство: {device}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора устройства: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Запускаем в отдельном потоке, чтобы не блокировать UI
                Task.Run(() =>
                {
                    sound.StartDetect(0); // или сохраняйте выбранный device
                });

                btnStartDetection.Enabled = false;
                btnStopDetection.Enabled = true;
                btnSelectDevice.Enabled = false;

                MessageBox.Show("Тюнер запущен! Играйте ноты...", "Запуск",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска тюнера: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // ★★★★ ДОБАВЬТЕ МЕТОД STOP В SOUND ★★★★
            sound.StopDetection();

            btnStartDetection.Enabled = true;
            btnStopDetection.Enabled = false;
            btnSelectDevice.Enabled = true;

            MessageBox.Show("Тюнер остановлен", "Остановка",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (formLoading) return;
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView2.Rows.Count) return;

            var row = dataGridView2.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            string note = row.Cells[0].Value?.ToString();
            string key = row.Cells[2].Value?.ToString();

            Console.WriteLine($"[Form1] Minecraft mapping: {note} -> {key}");

            if (!string.IsNullOrEmpty(note))
            {
                // ★★★★ СОХРАНЯЕМ В СЛОВАРЬ MINECRAFT ★★★★
                sound.NoteToKeyMinecraft[note] = key;
            }
        }
    }
    public class SaveData
    {
        public List<RowData> WindowsMappings { get; set; }
        public List<RowData> MinecraftMappings { get; set; }
    }

    public class RowData
    {
        public string Note { get; set; }
        public string Freq { get; set; }
        public string Key { get; set; }
    }
}
