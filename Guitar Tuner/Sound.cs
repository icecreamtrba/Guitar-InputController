using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Guitar_Tuner
{
    public class Sound
    {
        public Form1 gui;
        private BufferedWaveProvider bufferedWaveProvider = null;
        public IControlManager CurrentControlManager { get; set; }
        public WindowsControlManager WindowsManager { get; private set; }
        public MinecraftControlManager MinecraftManager { get; private set; }
        // ★★★★ РАЗДЕЛЬНЫЕ СЛОВАРИ ДЛЯ КАЖДОГО РЕЖИМА ★★★★
        public Dictionary<string, string> NoteToKeyWindows { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NoteToKeyMinecraft { get; private set; } = new Dictionary<string, string>();

        // ★★★★ СВОЙСТВО ДЛЯ ТЕКУЩЕГО СЛОВАРЯ ★★★★
        public Dictionary<string, string> CurrentNoteToKey
        {
            get
            {
                if (CurrentControlManager is MinecraftControlManager)
                    return NoteToKeyMinecraft;
                else
                    return NoteToKeyWindows;
            }
        }
        // словарь базовых частот
        public readonly Dictionary<string, float> noteBaseFreqs = new Dictionary<string, float>()
        {
            { "C", 16.35f }, { "C#", 17.32f }, { "D", 18.35f }, { "Eb", 19.45f },
            { "E", 20.60f }, { "F", 21.83f }, { "F#", 23.12f }, { "G", 24.50f },
            { "G#", 25.96f }, { "A", 27.50f }, { "Bb", 29.14f }, { "B", 30.87f },
        };

        // очередь для сглаживания
        private readonly Queue<float> freqHistory = new Queue<float>();
        private const int SmoothWindow = 3;

        // контроль частоты нажатий
        private readonly Dictionary<string, DateTime> lastHit = new Dictionary<string, DateTime>();
        private const int KeyCooldownMs = 300;

        private WaveInEvent currentWaveIn;
        private volatile bool isDetecting = false;

        public void StopDetection()
        {
            isDetecting = false;
            currentWaveIn?.StopRecording();
            currentWaveIn?.Dispose();
            currentWaveIn = null;
        }

        public Sound(MinecraftMod minecraftMod = null)
        {
            WindowsManager = new WindowsControlManager();

            // ★★★★ СОЗДАВАЙТЕ МЕНЕДЖЕР ДАЖЕ ЕСЛИ ПОДКЛЮЧЕНИЕ НЕ УДАЛОСЬ ★★★★
            if (minecraftMod != null)
            {
                MinecraftManager = new MinecraftControlManager(minecraftMod);
                Console.WriteLine($"[Sound] MinecraftManager created");
                Console.WriteLine($"[Sound] MinecraftMod provided: {minecraftMod != null}");
                Console.WriteLine($"[Sound] MinecraftMod connected: {minecraftMod.IsConnected}");
            }
            else
            {
                Console.WriteLine($"[Sound] No MinecraftMod provided - minecraftMod is null");
                MinecraftManager = null;
            }

            CurrentControlManager = WindowsManager;

            Console.WriteLine($"[Sound] Initialized - Windows: {WindowsManager != null}, Minecraft: {MinecraftManager != null}");
        }
        public void UpdateMinecraftMod(MinecraftMod mod)
        {
            if (mod != null)
            {
                MinecraftManager = new MinecraftControlManager(mod);
                Console.WriteLine($"[Sound] MinecraftManager updated");
            }
        }
        public void SwitchToWindowsMode()
        {
            CurrentControlManager = WindowsManager;
            Console.WriteLine("[Sound] Switched to Windows control mode");
            gui?.UpdateControlModeDisplay(); // Обновляем интерфейс
        }

        public void SwitchToMinecraftMode()
        {
            // ★★★★ ДОБАВЬТЕ ПОДРОБНУЮ ОТЛАДКУ ★★★★
            Console.WriteLine($"[Sound] SwitchToMinecraftMode called");
            Console.WriteLine($"[Sound] MinecraftManager: {MinecraftManager != null}");

            if (MinecraftManager != null)
            {
                Console.WriteLine($"[Sound] MinecraftManager.IsConnected: {MinecraftManager.IsConnected}");
                Console.WriteLine($"[Sound] MinecraftManager.IsEnabled: {MinecraftManager.IsEnabled}");
            }

            if (MinecraftManager != null && MinecraftManager.IsConnected && MinecraftManager.IsEnabled)
            {
                CurrentControlManager = MinecraftManager;
                Console.WriteLine("[Sound] Switched to Minecraft control mode");
                gui?.UpdateControlModeDisplay();
            }
            else
            {
                string reason = "неизвестная причина";
                if (MinecraftManager == null) reason = "MinecraftManager не создан";
                else if (!MinecraftManager.IsConnected) reason = "Minecraft не подключен";
                else if (!MinecraftManager.IsEnabled) reason = "MinecraftManager отключен";

                Console.WriteLine($"[Sound] Minecraft mod not available: {reason}");
                MessageBox.Show($"Minecraft мод не доступен: {reason}");
            }
        }
        public int SelectInputDevice()
        {
            int inputDevice = 0;
            bool isValidChoice = false;

            do
            {
                Console.Clear();
                Console.WriteLine("Please select input or recording device: ");

                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                    Console.WriteLine($"{i}. {WaveInEvent.GetCapabilities(i).ProductName}");

                Console.WriteLine();

                if (int.TryParse(Console.ReadLine(), out inputDevice))
                {
                    var caps = WaveInEvent.GetCapabilities(inputDevice);
                    Console.WriteLine("Channels: " + caps.Channels);
                    isValidChoice = true;
                    Console.WriteLine("You have chosen " + caps.ProductName + ".\n");
                    //gui.sound = this;
                }
                else isValidChoice = false;

            } while (!isValidChoice);

            return inputDevice;
        }

        public void StartDetect(int inputDevice)
        {
            isDetecting = true;

            currentWaveIn = new WaveInEvent
            {
                DeviceNumber = inputDevice
            };

            var caps = WaveInEvent.GetCapabilities(inputDevice);
            currentWaveIn.WaveFormat = new WaveFormat(44100, caps.Channels);
            currentWaveIn.DataAvailable += WaveIn_DataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(currentWaveIn.WaveFormat);

            currentWaveIn.StartRecording();

            IWaveProvider stream = caps.Channels > 1
                ? new Wave16ToFloatProvider(new StereoToMonoProvider16(bufferedWaveProvider))
                : new Wave16ToFloatProvider(bufferedWaveProvider);

            Pitch pitch = new Pitch(stream);
            byte[] buffer = new byte[8192];
            int bytesRead;

            Console.WriteLine("Play or sing a note! Press ESC to exit at any time.\n");

            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);

                float rawFreq = pitch.Get(buffer);
                float freq = SmoothFrequency(rawFreq);

                if (freq > 0)
                {
                    var (note, noteFreq) = GetNoteWithFreq(freq);
                    if (note != null && noteFreq > 0)
                    {
                        float cents = 1200 * (float)Math.Log(freq / noteFreq, 2);
                        string message = $"Freq: {freq:F2} Hz | Note: {note} | Deviation: {cents:+0.00;-0.00} cents";
                        Console.WriteLine(message);

                        gui?.UpdateNoteAndFreq(note, freq.ToString());
                        if (gui != null && gui.InvokeRequired)
                        {
                            gui.Invoke(new Action(() =>
                            {
                                foreach (DataGridViewRow row in gui.dataGridView1.Rows)
                                    row.DefaultCellStyle.BackColor = Color.White;

                                var foundRow = gui.dataGridView1.Rows
                                    .Cast<DataGridViewRow>()
                                    .FirstOrDefault(r => r.Cells[0].Value.ToString() == note);

                                if (foundRow != null)
                                    foundRow.DefaultCellStyle.BackColor = Color.LightGreen;




                                foreach (DataGridViewRow row in gui.dataGridView2.Rows)
                                    row.DefaultCellStyle.BackColor = Color.White;

                                var foundRow2 = gui.dataGridView2.Rows
                                    .Cast<DataGridViewRow>()
                                    .FirstOrDefault(r => r.Cells[0].Value.ToString() == note);

                                if (foundRow2 != null)
                                    foundRow2.DefaultCellStyle.BackColor = Color.LightGreen;
                            }));
                        }
                        else
                        {
                            foreach (DataGridViewRow row in gui.dataGridView1.Rows)
                                row.DefaultCellStyle.BackColor = Color.White;

                            var foundRow = gui.dataGridView1.Rows
                                .Cast<DataGridViewRow>()
                                .FirstOrDefault(r => r.Cells[0].Value.ToString() == note);

                            if (foundRow != null)
                                foundRow.DefaultCellStyle.BackColor = Color.LightGreen;




                            foreach (DataGridViewRow row in gui.dataGridView2.Rows)
                                row.DefaultCellStyle.BackColor = Color.White;

                            var foundRow2 = gui.dataGridView2.Rows
                                .Cast<DataGridViewRow>()
                                .FirstOrDefault(r => r.Cells[0].Value.ToString() == note);

                            if (foundRow2 != null)
                                foundRow2.DefaultCellStyle.BackColor = Color.LightGreen;
                        }
                        // --- эмуляция клавиш через GUI-поток ---
                        TriggerKey(note);
                    }
                }

            } while (bytesRead != 0 && isDetecting &&
                !(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape));

            if (isDetecting)
            {
                currentWaveIn.StopRecording();
                currentWaveIn.Dispose();
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bufferedWaveProvider?.AddSamples(e.Buffer, 0, e.BytesRecorded);
            bufferedWaveProvider.DiscardOnBufferOverflow = true;
        }

        private float SmoothFrequency(float newFreq)
        {
            if (newFreq <= 0) return 0;

            freqHistory.Enqueue(newFreq);
            if (freqHistory.Count > SmoothWindow)
                freqHistory.Dequeue();

            return freqHistory.Average();
        }

        public (string note, float noteFreq) GetNoteWithFreq(float freq)
        {
            foreach (var note in noteBaseFreqs)
            {
                float baseFreq = note.Value;

                for (int i = 0; i < 9; i++)
                {
                    float noteFreq = baseFreq * (float)Math.Pow(2, i);
                    if (Math.Abs(freq - noteFreq) < 1.0f) return (note.Key + i, noteFreq);
                }
            }
            return (null, 0);
        }

        // --- новый TriggerKey с gui.Invoke ---
        private void TriggerKey(string note)
        {
            // ★★★★ ИСПОЛЬЗУЕМ ТЕКУЩИЙ СЛОВАРЬ ★★★★
            if (!CurrentNoteToKey.TryGetValue(note, out string action) || string.IsNullOrEmpty(action))
            {
                Console.WriteLine($"[Sound] No mapping for note: {note} in current mode");
                return;
            }

            Console.WriteLine($"[Sound] TriggerKey: {note} -> {action}");
            Console.WriteLine($"[Sound] CurrentControlManager: {CurrentControlManager?.GetType().Name}");

            CurrentControlManager?.HandleNote(action);
        }


    }
}
