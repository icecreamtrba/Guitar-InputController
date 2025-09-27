using System;
using System.Runtime.InteropServices;

namespace Guitar_Tuner
{
    public static class MouseSimulator
    {
        public static float mouseSpeed = 1f;

        [Flags]
        private enum MouseEventFlags : uint
        {
            MOVE = 0x0001,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            WHEEL = 0x0800,
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        // ====== Клики ======
        public static void LeftClick() => Click(MouseEventFlags.LEFTDOWN, MouseEventFlags.LEFTUP);
        public static void RightClick() => Click(MouseEventFlags.RIGHTDOWN, MouseEventFlags.RIGHTUP);
        public static void MiddleClick() => Click(MouseEventFlags.MIDDLEDOWN, MouseEventFlags.MIDDLEUP);

        private static void Click(MouseEventFlags down, MouseEventFlags up)
        {
            mouse_event(down | up, 0, 0, 0, UIntPtr.Zero);
        }

        // ====== Движение мыши ======
        public static void Move(int deltaX, int deltaY)
        {
            if (GetCursorPos(out POINT p))
            {
                SetCursorPos(p.X + deltaX * (int)mouseSpeed, p.Y + deltaY * (int)mouseSpeed);
            }
        }

        // ====== Скролл ======
        public static void Scroll(int delta) // delta >0 вверх, <0 вниз
        {
            mouse_event(MouseEventFlags.WHEEL, 0, 0, (uint)delta, UIntPtr.Zero);
        }
    }
}
