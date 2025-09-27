using System;
using System.Runtime.InteropServices;

namespace Guitar_Tuner
{
    public static class KeyboardSimulator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;       // виртуальный код клавиши
            public ushort wScan;     // scan code клавиши
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// Нажать и отпустить клавишу через ScanCode (надежнее для любых окон)
        /// </summary>
        /// <param name="keyCode">виртуальный код клавиши (0x41 = A, 0x31 = 1 и т.д.)</param>
        public static void PressKey(ushort keyCode)
        {
            ushort scanCode = (ushort)MapVirtualKey(keyCode, 0);

            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = scanCode;
            inputs[0].U.ki.dwFlags = KEYEVENTF_SCANCODE;
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;

            // Key up
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = scanCode;
            inputs[1].U.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;
            inputs[1].U.ki.time = 0;
            inputs[1].U.ki.dwExtraInfo = IntPtr.Zero;

            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                Console.WriteLine($"SendInput failed for keyCode 0x{keyCode:X}");
            }
        }
    }
}
