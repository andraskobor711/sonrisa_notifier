using System.Threading;
using System.Threading.Tasks;
using Sonrisa.Notifier.Core.Models;

namespace Sonrisa.Notifier.Core.Interfaces
{
    public interface INotificationSender
    {
        Task<DeliveryResult> SendAsync(OutgoingMessage message, CancellationToken ct = default);
    }
}
