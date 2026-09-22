namespace Sonrisa.Notifier.Core.Models
{
    public class DeliveryResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty; // Sent, Failed
        public string? ProviderMessageId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
