using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;

namespace MonitorServer.Controllers
{
    public class ServerController
    {
        private readonly TcpListener _listener;
        private readonly IMetricCollector _metricCollector;

        // Chia 2 list riêng biệt để dễ quản lý theo Role
        private readonly List<TcpClient> _adminClients = new List<TcpClient>();
        private readonly List<TcpClient> _standardClients = new List<TcpClient>();
        private readonly object _lockObj = new object(); // Dùng để lock khi thao tác với List trong môi trường Multi-thread

        public ServerController(string ip, int port, IMetricCollector metricCollector)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _metricCollector = metricCollector;
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server đã khởi động. Đang lắng nghe kết nối...");

            // Thread riêng biệt để liên tục gửi Metrics cho Admin
            Thread broadcastThread = new Thread(BroadcastMetricsLoop) { IsBackground = true };
            broadcastThread.Start();

            // Vòng lặp chính nhận kết nối
            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                Console.WriteLine($"[+] Client kết nối: {client.Client.RemoteEndPoint}");

                // Cấp cho mỗi client 1 Thread riêng để xử lý non-blocking
                Thread clientThread = new Thread(() => HandleClient(client)) { IsBackground = true };
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            string role = "Unknown";

            try
            {
                while (client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Client ngắt kết nối

                    string jsonText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    NetworkPacket? packet = JsonSerializer.Deserialize<NetworkPacket>(jsonText);

                    if (packet != null)
                    {
                        if (packet.Type == PacketType.Auth)
                        {
                            role = packet.Role;
                            AddClientToList(client, role);
                        }
                        else if (packet.Type == PacketType.DummyTraffic)
                        {
                            // Chỉ nhận để tính băng thông, không cần xử lý logic
                        }
                    }
                }
            }
            catch (Exception) { /* Bỏ qua lỗi kết nối đứt ngang */ }
            finally
            {
                RemoveClientFromList(client, role);
                client.Close();
                Console.WriteLine($"[-] Client ngắt kết nối.");
            }
        }

        private void AddClientToList(TcpClient client, string role)
        {
            lock (_lockObj)
            {
                if (role == "Admin") _adminClients.Add(client);
                else if (role == "Standard") _standardClients.Add(client);
            }
            Console.WriteLine($"Đã xác thực một {role} Client.");
        }

        private void RemoveClientFromList(TcpClient client, string role)
        {
            lock (_lockObj)
            {
                if (role == "Admin") _adminClients.Remove(client);
                else if (role == "Standard") _standardClients.Remove(client);
            }
        }

        private void BroadcastMetricsLoop()
        {
            while (true)
            {
                Thread.Sleep(1000);

                NetworkMetrics metrics;
                List<TcpClient> adminsToNotify;

                lock (_lockObj)
                {
                    metrics = _metricCollector.GetCurrentMetrics(_standardClients.Count, _adminClients.Count);
                    adminsToNotify = _adminClients.ToList();
                }

                if (adminsToNotify.Count > 0)
                {
                    NetworkPacket packet = new NetworkPacket
                    {
                        Type = PacketType.MetricsUpdate,
                        Payload = JsonSerializer.Serialize(metrics)
                    };

                    byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));

                    foreach (var admin in adminsToNotify)
                    {
                        try
                        {
                            admin.GetStream().Write(data, 0, data.Length);
                        }
                        catch 
                        { 
                            /* Xử lý nếu gửi thất bại */ 
                        }
                    }
                }
            }
        }
    }
}