using System.Diagnostics;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;

namespace MonitorServer.Services
{
    public class PerformanceCounterService : IMetricCollector
    {
        private PerformanceCounter? _bytesReceivedCounter;
        private PerformanceCounter? _bytesSentCounter;

        public PerformanceCounterService()
        {
            try
            {
                // Lấy danh sách tất cả các card mạng có trên máy
                var category = new PerformanceCounterCategory("Network Interface");
                string[] instances = category.GetInstanceNames();

                if (instances.Length > 0)
                {
                    // Tự động tìm card mạng thực tế (bỏ qua các card ảo nội bộ như Loopback/isatap)
                    string instanceName = instances.FirstOrDefault(name =>
                        !name.Contains("Loopback", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("isatap", StringComparison.OrdinalIgnoreCase))
                        ?? instances[0];

                    Console.WriteLine($"[+] Đã tìm thấy và theo dõi card mạng: {instanceName}");

                    _bytesReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instanceName);
                    _bytesSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instanceName);
                }
                else
                {
                    Console.WriteLine("[Cảnh báo] Không tìm thấy card mạng nào trên hệ thống.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cảnh báo] Lỗi khởi tạo Performance Counter: {ex.Message}");
            }
        }

        public NetworkMetrics GetCurrentMetrics(int standardClientsCount, int adminClientsCount)
        {
            float downloadSpeed = 0;
            float uploadSpeed = 0;

            try
            {
                // Gọi NextValue() bên trong try-catch để đảm bảo server không sập nếu card mạng bị ngắt đột ngột
                if (_bytesReceivedCounter != null && _bytesSentCounter != null)
                {
                    downloadSpeed = _bytesReceivedCounter.NextValue() / 1024f; // Chuyển sang KBps
                    uploadSpeed = _bytesSentCounter.NextValue() / 1024f;
                }
            }
            catch (InvalidOperationException)
            {
                // Nuốt lỗi nếu card mạng tạm thời mất kết nối
            }

            return new NetworkMetrics
            {
                DownloadSpeedKbps = downloadSpeed,
                UploadSpeedKbps = uploadSpeed,
                ActiveStandardClients = standardClientsCount,
                ActiveAdminClients = adminClientsCount,
                Timestamp = DateTime.Now
            };
        }
    }
}