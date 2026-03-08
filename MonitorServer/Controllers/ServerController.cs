using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using SharedLibrary;
using SharedLibrary.Models;

namespace MonitorServer.Controllers
{
    public class ServerController
    {
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _adminClients = new List<TcpClient>();

        // Dictionary lưu trữ Metrics thật từ các client
        private readonly ConcurrentDictionary<string, ClientNetworkInfo> _clientMetrics = new ConcurrentDictionary<string, ClientNetworkInfo>();
        private readonly object _lockObj = new object();

        public ServerController()
        {
            _listener = new TcpListener(IPAddress.Parse(Constants.SERVER_IP), Constants.SERVER_PORT);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server đã khởi động tại {Constants.SERVER_IP}:{Constants.SERVER_PORT}.");

            Thread broadcastThread = new Thread(BroadcastMetricsToAdmins) { IsBackground = true };
            broadcastThread.Start();

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client)) { IsBackground = true };
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[8192];
            string role = "Unknown";
            string clientId = string.Empty;

            try
            {
                while (client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string jsonText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    NetworkPacket? packet = JsonSerializer.Deserialize<NetworkPacket>(jsonText);

                    if (packet != null)
                    {
                        if (packet.Type == PacketType.Auth)
                        {
                            role = packet.Role;
                            clientId = packet.ClientId;
                            if (role == "Admin")
                            {
                                lock (_lockObj) _adminClients.Add(client);
                                Console.WriteLine($"[+] Admin kết nối: {clientId}");
                            }
                        }
                        else if (packet.Type == PacketType.ClientMetricsReport && role == "Standard")
                        {
                            // Nhận dữ liệu thật từ Client và cập nhật vào Dictionary
                            var metrics = JsonSerializer.Deserialize<ClientNetworkInfo>(packet.Payload);
                            if (metrics != null)
                            {
                                _clientMetrics[metrics.ClientId] = metrics;
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (role == "Admin") lock (_lockObj) _adminClients.Remove(client);
                if (role == "Standard" && !string.IsNullOrEmpty(clientId))
                {
                    _clientMetrics.TryRemove(clientId, out _);
                }
                client.Close();
                Console.WriteLine($"[-] Client ngắt kết nối: {clientId}");
            }
        }

        private void BroadcastMetricsToAdmins()
        {
            while (true)
            {
                Thread.Sleep(1000); // Broadcast mỗi giây

                List<TcpClient> adminsToNotify;
                lock (_lockObj) adminsToNotify = _adminClients.ToList();

                if (adminsToNotify.Count > 0)
                {
                    // Lấy toàn bộ Value trong Dictionary chuyển thành List
                    var allMetrics = _clientMetrics.Values.ToList();

                    NetworkPacket packet = new NetworkPacket
                    {
                        Type = PacketType.AdminDashboardUpdate,
                        Payload = JsonSerializer.Serialize(allMetrics)
                    };

                    byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));

                    foreach (var admin in adminsToNotify)
                    {
                        try { admin.GetStream().Write(data, 0, data.Length); }
                        catch { }
                    }
                }
            }
        }
    }
}