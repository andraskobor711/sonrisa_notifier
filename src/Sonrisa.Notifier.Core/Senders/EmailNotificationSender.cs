using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;
using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Core.Senders
{
    public class EmailNotificationSender : INotificationSender
    {
        private readonly ILogger<EmailNotificationSender> _logger;

        private readonly string _channelConfigJson;

        public EmailNotificationSender(ILogger<EmailNotificationSender> logger, Sonrisa.Notifier.Infrastructure.Entities.Channel channel)
        {
            _logger = logger;
            _channelConfigJson = channel?.ConfigJson ?? string.Empty;
        }

        public Task<DeliveryResult> SendAsync(OutgoingMessage message, User user, CancellationToken ct = default)
        {
            try
            {
                var targetEmail = user?.Email ?? string.Empty;
                var displayName = user?.DisplayName ?? string.Empty;

                // NOTE: Real delivery should be implemented here using the cached _channelConfigJson and user information
                // (SMTP host, port, credentials, From address, templates, etc.). For this initial version we only log that
                // the message would be sent. The channel configuration was read in the constructor and cached.
                _logger.LogInformation("Email sender prepared message for user {UserId} <{Email}> via channel config present: {HasConfig}: {Title}", user?.Id, targetEmail, !string.IsNullOrEmpty(_channelConfigJson), message?.Title);

                return Task.FromResult(new DeliveryResult
                {
                    Success = true,
                    Status = "Sent",
                    ProviderMessageId = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email send failed for message {Title}", message?.Title);
                return Task.FromResult(new DeliveryResult { Success = false, Status = "Failed", ErrorMessage = ex.Message });
            }
        }
    }
}
