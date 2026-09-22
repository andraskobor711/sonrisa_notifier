using System;
using System.Collections.Generic;

namespace Sonrisa.Notifier.Core.Models
{
    public class OutgoingMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium";
        public List<string> Channels { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
