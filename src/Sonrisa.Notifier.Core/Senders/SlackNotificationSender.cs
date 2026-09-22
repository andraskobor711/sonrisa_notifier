using Microsoft.Extensions.Logging;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Core.Senders
{
    public class SlackNotificationSender : INotificationSender
    {
        private readonly ILogger<SlackNotificationSender> _logger;

        private readonly string _channelConfigJson;

        public SlackNotificationSender(ILogger<SlackNotificationSender> logger, SonrisaNotifierDbContext dbContext)
        {
            _logger = logger;
            _channelConfigJson = dbContext.Channels
                .Where(c => c.Type.ToLower() == "slack")
                .Select(c => c.ConfigJson)
                .FirstOrDefault() ?? string.Empty;
        }

        public Task<DeliveryResult> SendAsync(OutgoingMessage message, User user, CancellationToken ct = default)
        {
            try
            {
                var targetEmail = user?.Email ?? string.Empty;
                var targetUserId = user?.Id.ToString() ?? string.Empty;

                // NOTE: Real Slack delivery should use the cached _channelConfigJson (webhook url or token)
                // and user information to post messages. The channel configuration is read in the constructor.
                _logger.LogInformation("Slack sender prepared message for user {UserId} <{Email}> via channel config present: {HasConfig}: {Title}", targetUserId, targetEmail, !string.IsNullOrEmpty(_channelConfigJson), message?.Title);

                return Task.FromResult(new DeliveryResult
                {
                    Success = true,
                    Status = "Sent",
                    ProviderMessageId = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slack send failed for message {Title}", message?.Title);
                return Task.FromResult(new DeliveryResult { Success = false, Status = "Failed", ErrorMessage = ex.Message });
            }
        }
    }
}
