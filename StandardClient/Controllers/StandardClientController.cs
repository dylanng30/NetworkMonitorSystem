using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using SharedLibrary;
using SharedLibrary.Models;

namespace StandardClient.Controllers
{
    public class StandardClientController
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected;
        private Thread? _monitorThread;
        private string _clientId = string.Empty;

        public event Action<string>? OnStatusChanged;
        public event Action<double, double>? OnSpeedUpdated; // Để hiển thị lên UI của Client (nếu cần)

        public void Connect()
        {
            try
            {
                _client = new TcpClient(Constants.SERVER_IP, Constants.SERVER_PORT);
                _stream = _client.GetStream();
                _isConnected = true;
                _clientId = "Std_" + Guid.NewGuid().ToString().Substring(0, 5);

                NetworkPacket authPacket = new NetworkPacket
                {
                    Type = PacketType.Auth,
                    Role = "Standard",
                    ClientId = _clientId
                };
                SendPacket(authPacket);

                OnStatusChanged?.Invoke($"Đã kết nối Server {Constants.SERVER_IP}:{Constants.SERVER_PORT}!");

                // Bắt đầu luồng đo băng thông thực tế
                _monitorThread = new Thread(MonitorNetworkTraffic) { IsBackground = true };
                _monitorThread.Start();
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Lỗi kết nối: {ex.Message}");
            }
        }

        private void MonitorNetworkTraffic()
        {
            // Lấy IP và Port cục bộ của Socket đang kết nối tới Server
            var localEndPoint = _client?.Client.LocalEndPoint as IPEndPoint;
            string localIp = localEndPoint?.Address.ToString() ?? "Unknown";
            int localPort = localEndPoint?.Port ?? 0;

            // Để tính tốc độ, cần lưu lại số byte cũ
            long oldBytesReceived = GetTotalBytesReceived();
            long oldBytesSent = GetTotalBytesSent();

            while (_isConnected)
            {
                Thread.Sleep(1000); // Cập nhật mỗi giây

                long newBytesReceived = GetTotalBytesReceived();
                long newBytesSent = GetTotalBytesSent();

                // Tính toán KB/s
                double downloadSpeedKbps = (newBytesReceived - oldBytesReceived) / 1024.0;
                double uploadSpeedKbps = (newBytesSent - oldBytesSent) / 1024.0;

                oldBytesReceived = newBytesReceived;
                oldBytesSent = newBytesSent;

                OnSpeedUpdated?.Invoke(downloadSpeedKbps, uploadSpeedKbps);

                var metrics = new ClientNetworkInfo
                {
                    ClientId = _clientId,
                    IpAddress = localIp,
                    Port = localPort,
                    DownloadSpeedKbps = downloadSpeedKbps,
                    UploadSpeedKbps = uploadSpeedKbps,
                    LastUpdated = DateTime.Now
                };

                SendPacket(new NetworkPacket
                {
                    Type = PacketType.ClientMetricsReport,
                    ClientId = _clientId,
                    Payload = JsonSerializer.Serialize(metrics)
                });
            }
        }

        private long GetTotalBytesReceived() => NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Sum(nic => nic.GetIPv4Statistics().BytesReceived);

        private long GetTotalBytesSent() => NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Sum(nic => nic.GetIPv4Statistics().BytesSent);

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
                catch { Disconnect(); }
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            OnStatusChanged?.Invoke("Đã ngắt kết nối.");
        }
    }
}