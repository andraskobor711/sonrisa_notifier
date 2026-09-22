using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Core.Interfaces
{
    public interface INotificationSenderFactory
    {
        INotificationSender? GetSender(string channelType);
    }
}
