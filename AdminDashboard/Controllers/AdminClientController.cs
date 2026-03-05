using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SharedLibrary.Models;

namespace AdminDashboard.Controllers
{
    public class AdminClientController
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private Thread? _listenThread;
        private bool _isConnected;

        // Sự kiện (Event) để báo cho UI biết khi có dữ liệu mới
        public event Action<NetworkMetrics>? OnMetricsReceived;

        public void Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;

                // 1. Gửi gói tin xác thực Role là Admin
                NetworkPacket authPacket = new NetworkPacket
                {
                    Type = PacketType.Auth,
                    Role = "Admin",
                    ClientId = "Admin_" + Guid.NewGuid().ToString().Substring(0, 5)
                };
                SendPacket(authPacket);

                // 2. Mở luồng chạy ngầm để lắng nghe dữ liệu từ Server liên tục
                _listenThread = new Thread(ListenForData) { IsBackground = true };
                _listenThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kết nối Server: {ex.Message}");
            }
        }

        private void SendPacket(NetworkPacket packet)
        {
            if (_stream != null && _isConnected)
            {
                string json = JsonSerializer.Serialize(packet);
                byte[] data = Encoding.UTF8.GetBytes(json);
                _stream.Write(data, 0, data.Length);
            }
        }

        private void ListenForData()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (_isConnected && _stream != null)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Server ngắt kết nối

                    string jsonText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    NetworkPacket? packet = JsonSerializer.Deserialize<NetworkPacket>(jsonText);

                    if (packet != null && packet.Type == PacketType.MetricsUpdate)
                    {
                        // Giải mã Payload chứa thông số mạng
                        NetworkMetrics? metrics = JsonSerializer.Deserialize<NetworkMetrics>(packet.Payload);
                        if (metrics != null)
                        {
                            // Bắn event ra ngoài cho View cập nhật
                            OnMetricsReceived?.Invoke(metrics);
                        }
                    }
                }
            }
            catch
            {
                _isConnected = false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}