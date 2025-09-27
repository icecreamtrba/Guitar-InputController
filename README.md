# Console Pitch Tuner with Key & Mouse Automation

A very basic **C# .NET Console-based pitch tuner** designed as an exercise in learning **digital signal processing**.

---

## Features

- Detects input from your **voice** or musical instruments (guitar, etc.).
- Real-time **note detection** with frequency and deviation display.
- **DataGrid** mapping: assign specific keyboard keys or mouse actions to notes.
- When the detected tone matches a note in the DataGrid:
  - The corresponding row turns **green**.
  - Simulates a press of the assigned key or performs the mouse action.

---

## Usage

1. **Compile** and run the `.exe`.
2. **Select your input or recording device** when prompted.
3. Play or sing a note.
4. Observe the console output and DataGrid highlighting.
5. Assign actions in the DataGrid:
   - **Keyboard keys**: use virtual key codes (`0x41`) or special keys in braces: `{ENTER}`, `{DEL}`, `{INS}`, etc.
   - **Mouse actions**: use keywords like:
     - `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`
     - `LClick`, `RClick`, `MClick`
     - `ScrollDown`, `ScrollUp`

> Tip: try it with Notepad or a text field to see the keyboard actions in real time.

---

## Notes

- Supports **up to the 4th octave** (C0 to B4) by default in the DataGrid.
- Uses **NAudio** for audio capture and pitch detection.
- Designed for learning purposes; can be extended with more sophisticated DSP or UI.

---

> Developed with help from ChatGPT.