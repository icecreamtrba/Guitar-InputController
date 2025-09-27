using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Guitar_Tuner
{
    public class Sound
    {
        public Form1 gui;
        private BufferedWaveProvider bufferedWaveProvider = null;


        public Dictionary<string, string> NoteToKey { get; private set; } = new Dictionary<string, string>();
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
            WaveInEvent waveIn = new WaveInEvent
            {
                DeviceNumber = inputDevice
            };

            var caps = WaveInEvent.GetCapabilities(inputDevice);
            waveIn.WaveFormat = new WaveFormat(44100, caps.Channels);
            waveIn.DataAvailable += WaveIn_DataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);

            waveIn.StartRecording();

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
                        }
                        // --- эмуляция клавиш через GUI-поток ---
                        TriggerKey(note);
                    }
                }

            } while (bytesRead != 0 && !(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape));

            waveIn.StopRecording();
            waveIn.Dispose();
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
            if (!NoteToKey.TryGetValue(note, out string action) || string.IsNullOrEmpty(action))
                return; // ничего не назначено

            if (!lastHit.ContainsKey(note) || (DateTime.Now - lastHit[note]).TotalMilliseconds > KeyCooldownMs)
            {
                Console.WriteLine($"[DEBUG] TriggerKey called for {note} -> {action}");

                gui?.Invoke(new Action(() =>
                {
                    switch (action)
                    {
                        // --- клавиши ---
                        case "LClick":
                            MouseSimulator.LeftClick(); break;
                        case "RClick":
                            MouseSimulator.RightClick(); break;
                        case "MClick":
                            MouseSimulator.MiddleClick(); break;

                        case "MoveUp":
                            MouseSimulator.Move(0, -10); break;
                        case "MoveDown":
                            MouseSimulator.Move(0, 10); break;
                        case "MoveLeft":
                            MouseSimulator.Move(-10, 0); break;
                        case "MoveRight":
                            MouseSimulator.Move(10, 0); break;

                        case "ScrollUp":
                            MouseSimulator.Scroll(120); break;
                        case "ScrollDown":
                            MouseSimulator.Scroll(-120); break;

                        default: // если это клавиша, например "0x41"
                            SendKeys.SendWait(action); break;
                    }
                }));

                lastHit[note] = DateTime.Now;
            }
        }


    }
}
