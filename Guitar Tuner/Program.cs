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
            Sound sound = new Sound(); // ещё без gui

            Form1 gui = null;

            Thread guiThread = new Thread(() =>
            {
                gui = new Form1(sound);
                sound.gui = gui; // сразу после создания формы
                Application.Run(gui);
            });
            guiThread.SetApartmentState(ApartmentState.STA);
            guiThread.IsBackground = true;
            guiThread.Start();

            // ждём пока форма полностью инициализируется
            while (gui == null || !gui.IsHandleCreated)
                Thread.Sleep(50);

            int device = sound.SelectInputDevice();
            sound.StartDetect(device);

            guiThread.Join();
        }
    }
}