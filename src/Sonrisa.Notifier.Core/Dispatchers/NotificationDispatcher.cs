using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;
using Sonrisa.Notifier.Infrastructure;

namespace Sonrisa.Notifier.Core.Dispatchers
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly SonrisaNotifierDbContext _db;
        private readonly INotificationSenderFactory _factory;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(SonrisaNotifierDbContext db, INotificationSenderFactory factory, ILogger<NotificationDispatcher> logger)
        {
            _db = db;
            _factory = factory;
            _logger = logger;
        }

        public async Task DispatchAsync(OutgoingMessage message, CancellationToken ct = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var channelsWithUsers = await _db.UsersChannels.GroupBy(uc => uc.ChannelId)
                                                           .ToListAsync(ct);

            foreach (var channelWithUsers in channelsWithUsers)
            {
                var channel = _db.Channels.FirstOrDefault(c => c.Id == channelWithUsers.Key);
                if (channel == null)
                {
                    _logger.LogWarning("Channel with ID {ChannelId} not found in database.", channelWithUsers.Key);
                    continue;
                }

                var sender = _factory.GetSender(channel.Type);
                if (sender == null)
                {
                    _logger.LogWarning("No sender available for channel type {ChannelType}", channel.Type);
                    continue;
                }
                ;

                foreach (var userChannel in channelWithUsers)
                {
                    var user = _db.Users.FirstOrDefault(u => u.Id == userChannel.UserId);
                    if (user?.IsActive == true)
                    {
                        await sender.SendAsync(message, user, ct);
                    }
                }
            }
        }
    }
}
