using System.Threading;
using System.Threading.Tasks;
using Sonrisa.Notifier.Core.Models;
using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Core.Interfaces
{
    public interface INotificationSender
    {
        Task<DeliveryResult> SendAsync(OutgoingMessage message, User user, CancellationToken ct = default);
    }
}
