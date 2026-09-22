namespace Sonrisa.Notifier.Core.Interfaces
{
    public interface INotificationSenderFactory
    {
        INotificationSender? GetSender(string channelType);
    }
}
