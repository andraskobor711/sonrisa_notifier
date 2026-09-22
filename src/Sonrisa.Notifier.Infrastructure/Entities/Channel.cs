using System;

namespace Sonrisa.Notifier.Infrastructure.Entities
{
    public class Channel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // email, slack, etc.
        public string ConfigJson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
