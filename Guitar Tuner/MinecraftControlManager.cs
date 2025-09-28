using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guitar_Tuner
{
    public class MinecraftControlManager : IControlManager
    {
        public bool IsEnabled { get; set; } = true;
        private MinecraftMod minecraftMod; 

        private readonly Dictionary<string, DateTime> lastHit = new Dictionary<string, DateTime>();
        private const int KeyCooldownMs = 300;

        // ★★★★ ДОБАВЬТЕ PUBLIC PROPERTY ДЛЯ ПРОВЕРКИ ПОДКЛЮЧЕНИЯ ★★★★
        public MinecraftMod MinecraftMod => minecraftMod;
        public bool IsConnected => minecraftMod?.IsConnected == true;

        public MinecraftControlManager(MinecraftMod mod)
        {
            minecraftMod = mod;
        }

        public async void HandleNote(string note)
        {
            if (!IsEnabled || minecraftMod == null || !minecraftMod.IsConnected) return;

            if (!lastHit.ContainsKey(note) || (DateTime.Now - lastHit[note]).TotalMilliseconds > KeyCooldownMs)
            {
                Console.WriteLine($"[Minecraft] Sending note: {note}");

                try
                {
                    await minecraftMod.SendNoteAsync(note);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Minecraft] Error sending note: {ex.Message}");
                }

                lastHit[note] = DateTime.Now;
            }
        }
    }
}