using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class NetworkPacket
    {
        public PacketType Type { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
