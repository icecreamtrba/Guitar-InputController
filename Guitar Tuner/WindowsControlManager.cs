// WindowsControlManager.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Guitar_Tuner
{
    public class WindowsControlManager : IControlManager
    {
        public bool IsEnabled { get; set; } = true;

        private readonly Dictionary<string, DateTime> lastHit = new Dictionary<string, DateTime>();
        private const int KeyCooldownMs = 300;

        public void HandleNote(string note)
        {
            if (!IsEnabled) return;

            // Ваша существующая логика Windows управления
            if (!lastHit.ContainsKey(note) || (DateTime.Now - lastHit[note]).TotalMilliseconds > KeyCooldownMs)
            {
                Console.WriteLine($"[Windows] TriggerKey called for {note}");

                // Здесь ваша текущая логика эмуляции клавиш/мыши
                Application.OpenForms[0]?.Invoke(new Action(() =>
                {
                    // Ваш существующий код из TriggerKey
                    switch (note)
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

                        default:
                            SendKeys.SendWait(note); break;
                    }
                }));

                lastHit[note] = DateTime.Now;
            }
        }
    }
}