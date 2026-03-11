using System;

namespace SharedLibrary.Models
{
    public class AlertInfo
    {
        public long Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}