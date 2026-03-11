using System;

namespace SharedLibrary.Models
{
    public class ConnectionLogInfo
    {
        public long Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime EventTime { get; set; }
    }
}