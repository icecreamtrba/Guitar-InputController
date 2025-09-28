using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Guitar_Tuner
{
    public class MinecraftMod
    {
        private string host;
        private int port;
        private TcpClient client;
        private NetworkStream stream;

        public bool IsConnected => client != null && client.Connected;

        public MinecraftMod(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            client = new TcpClient();
            await client.ConnectAsync(host, port);
            stream = client.GetStream();
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            stream?.Close();
            client?.Close();
        }

        // ★★★★ ОСНОВНАЯ ОТПРАВКА КОМАНД ★★★★
        public async Task SendNoteAsync(string note)
        {
            if (!IsConnected) return;
            byte[] data = Encoding.UTF8.GetBytes(note + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }

        // ★★★★ ПЛАВНЫЕ ПОВОРОТЫ С НАСТРАИВАЕМОЙ СКОРОСТЬЮ ★★★★
        public async Task SendSmoothRotationAsync(string direction, int smoothness = 10)
        {
            if (!IsConnected) return;

            // Формат: "look_right:10" где 10 - количество шагов (чем больше, тем плавнее)
            string command = $"{direction}:{smoothness}";
            await SendNoteAsync(command);
        }

        // ★★★★ МЕЛКИЕ ПОВОРОТЫ ★★★★
        public async Task SendSmallRotationAsync(string direction)
        {
            if (!IsConnected) return;

            string command = direction + "_small";
            await SendNoteAsync(command);
        }

        // ★★★★ ТОЧНАЯ УСТАНОВКА УГЛОВ ★★★★
        public async Task SetExactRotationAsync(float yaw, float pitch)
        {
            if (!IsConnected) return;

            string command = $"set_rotation:{yaw}:{pitch}";
            await SendNoteAsync(command);
        }

        // ★★★★ НОВЫЕ МЕТОДЫ ДЛЯ РАЗДЕЛЕННЫХ ДЕЙСТВИЙ ★★★★
        public async Task SendAttackAsync()
        {
            await SendNoteAsync("attack");
        }

        public async Task SendMineAsync()
        {
            await SendNoteAsync("mine");
        }

        public async Task SendUseAsync()
        {
            await SendNoteAsync("use");
        }

        public async Task SendUseHoldAsync()
        {
            await SendNoteAsync("use_hold");
        }

        public async Task SendUseReleaseAsync()
        {
            await SendNoteAsync("use_release");
        }
    }
}