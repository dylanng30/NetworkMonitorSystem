using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class NetworkMetrics
    {
        public float DownloadSpeedKbps { get; set; }
        public float UploadSpeedKbps { get; set; }
        public int ActiveStandardClients { get; set; }
        public int ActiveAdminClients { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
