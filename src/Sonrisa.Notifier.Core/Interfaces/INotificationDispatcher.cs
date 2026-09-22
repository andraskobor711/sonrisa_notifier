using Sonrisa.Notifier.Core.Models;

namespace Sonrisa.Notifier.Core.Interfaces
{
    public interface INotificationDispatcher
    {
        Task DispatchAsync(OutgoingMessage message, CancellationToken ct = default);
    }
}
