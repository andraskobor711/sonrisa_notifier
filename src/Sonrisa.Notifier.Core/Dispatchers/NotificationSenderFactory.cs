using Microsoft.Extensions.DependencyInjection;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Senders;

namespace Sonrisa.Notifier.Core.Dispatchers
{
    public class NotificationSenderFactory : INotificationSenderFactory
    {
        private readonly IServiceProvider _sp;

        public NotificationSenderFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public INotificationSender? GetSender(string channelType)
        {
            if (channelType == null) return null;
            var type = channelType.ToLowerInvariant();
            return type switch
            {
                "email" => _sp.GetRequiredService<EmailNotificationSender>(),
                "slack" => _sp.GetRequiredService<SlackNotificationSender>(),
                _ => null
            };
        }
    }
}
