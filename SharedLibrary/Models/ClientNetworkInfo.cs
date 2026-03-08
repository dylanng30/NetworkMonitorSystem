using System;

namespace SharedLibrary.Models
{
    public class ClientNetworkInfo
    {
        public string ClientId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public double DownloadSpeedKbps { get; set; }
        public double UploadSpeedKbps { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}