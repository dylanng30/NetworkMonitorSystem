using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using SharedLibrary;
using SharedLibrary.Models;

namespace AdminDashboard.Controllers
{
    public class AdminClientController
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private Thread? _listenThread;
        private bool _isConnected;

        // Trả về một List chứa thông tin của tất cả StandardClients
        public event Action<List<ClientNetworkInfo>>? OnMetricsReceived;

        public void Connect()
        {
            try
            {
                _client = new TcpClient(Constants.SERVER_IP, Constants.SERVER_PORT);
                _stream = _client.GetStream();
                _isConnected = true;

                NetworkPacket authPacket = new NetworkPacket
                {
                    Type = PacketType.Auth,
                    Role = "Admin",
                    ClientId = "Admin_" + Guid.NewGuid().ToString().Substring(0, 5)
                };
                SendPacket(authPacket);

                _listenThread = new Thread(ListenForData) { IsBackground = true };
                _listenThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi: {ex.Message}");
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
            byte[] buffer = new byte[16384]; // Tăng buffer size để chứa list json lớn
            try
            {
                while (_isConnected && _stream != null)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string jsonText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    NetworkPacket? packet = JsonSerializer.Deserialize<NetworkPacket>(jsonText);

                    if (packet != null && packet.Type == PacketType.AdminDashboardUpdate)
                    {
                        var clientsMetrics = JsonSerializer.Deserialize<List<ClientNetworkInfo>>(packet.Payload);
                        if (clientsMetrics != null)
                        {
                            OnMetricsReceived?.Invoke(clientsMetrics);
                        }
                    }
                }
            }
            catch { _isConnected = false; }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}