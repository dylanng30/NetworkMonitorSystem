using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SharedLibrary.Models;

namespace StandardClient.Controllers
{
    public class StandardClientController
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected;
        private bool _isGeneratingTraffic;
        private Thread? _trafficThread;

        // Bắn event ra ngoài để UI biết trạng thái
        public event Action<string>? OnStatusChanged;

        public void Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;

                // Gửi gói tin định danh là Standard Client
                NetworkPacket authPacket = new NetworkPacket
                {
                    Type = PacketType.Auth,
                    Role = "Standard",
                    ClientId = "Std_" + Guid.NewGuid().ToString().Substring(0, 5)
                };
                SendPacket(authPacket);

                OnStatusChanged?.Invoke("Đã kết nối tới Server!");
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Lỗi kết nối: {ex.Message}");
            }
        }

        private void SendPacket(NetworkPacket packet)
        {
            if (_stream != null && _isConnected)
            {
                try
                {
                    string json = JsonSerializer.Serialize(packet);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    _stream.Write(data, 0, data.Length);
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        // Bắt đầu gửi lượng lớn dữ liệu để test băng thông
        public void StartTrafficGenerator(int speedMultiplier)
        {
            if (!_isConnected) return;

            _isGeneratingTraffic = true;
            _trafficThread = new Thread(() => TrafficLoop(speedMultiplier)) { IsBackground = true };
            _trafficThread.Start();

            OnStatusChanged?.Invoke("Đang gửi dữ liệu (Traffic Generating)...");
        }

        public void StopTrafficGenerator()
        {
            _isGeneratingTraffic = false;
            OnStatusChanged?.Invoke("Đã dừng gửi dữ liệu.");
        }

        private void TrafficLoop(int speedMultiplier)
        {
            // Tạo gói tin 10KB rác để đẩy qua mạng
            byte[] dummyData = new byte[1024 * 10];
            new Random().NextBytes(dummyData);

            NetworkPacket packet = new NetworkPacket
            {
                Type = PacketType.DummyTraffic,
                Payload = Convert.ToBase64String(dummyData)
            };

            while (_isGeneratingTraffic && _isConnected)
            {
                SendPacket(packet);
                // Điều chỉnh tốc độ gửi dựa vào speedMultiplier
                Thread.Sleep(Math.Max(1, 100 / speedMultiplier));
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            _isGeneratingTraffic = false;
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            OnStatusChanged?.Invoke("Đã ngắt kết nối khỏi Server.");
        }
    }
}