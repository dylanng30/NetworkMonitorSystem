using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using MonitorServer.Services;
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

        private readonly DatabaseService _dbService = new DatabaseService();

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

            Thread dbThread = new Thread(SaveMetricsToDatabase) { IsBackground = true };
            dbThread.Start();

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client)) { IsBackground = true };
                clientThread.Start();
            }
        }

        private void SaveMetricsToDatabase()
        {
            while (true)
            {
                Thread.Sleep(15000);

                List<ClientNetworkInfo> currentMetrics;
                int currentAdminCount = 0;

                lock (_lockObj)
                {
                    currentMetrics = _clientMetrics.Values.ToList();
                    currentAdminCount = _adminClients.Count;
                }

                if (currentMetrics.Count > 0)
                {
                    _dbService.SaveMetricsBatch(currentMetrics);

                    float totalDownload = (float)currentMetrics.Sum(m => m.DownloadSpeedKbps);
                    float totalUpload = (float)currentMetrics.Sum(m => m.UploadSpeedKbps);

                    _dbService.SaveSystemMetrics(totalDownload, totalUpload, currentMetrics.Count, currentAdminCount);

                    foreach (var metric in currentMetrics)
                    {
                        if (metric.DownloadSpeedKbps > 50000)
                        {
                            string msg = $"Băng thông tải xuống cao bất thường: {metric.DownloadSpeedKbps:N0} Kbps";
                            _dbService.SaveAlert(metric.ClientId, "HighBandwidth", msg);
                        }
                    }

                    Console.WriteLine($"[DB] Đã lưu {currentMetrics.Count} bản ghi máy trạm và trạng thái tổng.");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[8192];
            string role = "Unknown";
            string clientId = string.Empty;

            string ipAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

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

                            _dbService.LogConnectionEvent(clientId, role, "Connected");
                            if (role == "Standard")
                            {
                                _dbService.UpdateClientStatus(clientId, ipAddress, "Online");
                            }

                            if (role == "Admin")
                            {
                                lock (_lockObj) _adminClients.Add(client);
                                Console.WriteLine($"[+] Admin kết nối: {clientId}");
                            }
                        }
                        else if (packet.Type == PacketType.ClientMetricsReport && role == "Standard")
                        {
                            var metrics = JsonSerializer.Deserialize<ClientNetworkInfo>(packet.Payload);
                            if (metrics != null)
                            {
                                _clientMetrics[metrics.ClientId] = metrics;
                            }
                        }
                        else if (packet.Type == PacketType.RequestAlerts && role == "Admin")
                        {
                            var alerts = _dbService.GetRecentAlerts();
                            var response = new NetworkPacket
                            {
                                Type = PacketType.ResponseAlerts,
                                Payload = JsonSerializer.Serialize(alerts)
                            };
                            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                            stream.Write(data, 0, data.Length);
                        }
                        else if (packet.Type == PacketType.RequestConnectionLogs && role == "Admin")
                        {
                            var logs = _dbService.GetRecentLogs();
                            var response = new NetworkPacket
                            {
                                Type = PacketType.ResponseConnectionLogs,
                                Payload = JsonSerializer.Serialize(logs)
                            };
                            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (role == "Admin") lock (_lockObj) _adminClients.Remove(client);

                if (!string.IsNullOrEmpty(clientId))
                {
                    _dbService.LogConnectionEvent(clientId, role, "Disconnected");
                    if (role == "Standard")
                    {
                        _clientMetrics.TryRemove(clientId, out _);
                        _dbService.UpdateClientStatus(clientId, null, "Offline");
                    }
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