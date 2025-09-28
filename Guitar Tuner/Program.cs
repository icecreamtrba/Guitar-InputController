using System;
using System.Threading;
using System.Windows.Forms;

namespace Guitar_Tuner
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            // ★★★★ СОЗДАЕМ ФОРМУ БЕЗ ПЕРЕДАЧИ SOUND ★★★★
            Form1 gui = null;

            Thread guiThread = new Thread(() =>
            {
                gui = new Form1(); // ★★★★ БЕЗ ПАРАМЕТРОВ ★★★★
                Application.Run(gui);
            });
            guiThread.SetApartmentState(ApartmentState.STA);
            guiThread.IsBackground = true;
            guiThread.Start();

            // ждём пока форма полностью инициализируется
            while (gui == null || !gui.IsHandleCreated)
                Thread.Sleep(50);

            // ★★★★ ТЕПЕРЬ SOUND СОЗДАЕТСЯ В Form1_Load ★★★★
            // НЕ ВЫЗЫВАЕМ sound.SelectInputDevice() и sound.StartDetect() здесь
            // Это будет сделано через GUI

            guiThread.Join();
        }
    }
}