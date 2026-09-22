using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Core.Dispatchers;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class NotificationDispatcherTests
    {
        private SonrisaNotifierDbContext CreateContext(SqliteConnection conn)
        {
            var opts = new DbContextOptionsBuilder<SonrisaNotifierDbContext>()
                .UseSqlite(conn)
                .Options;

            var ctx = new SonrisaNotifierDbContext(opts);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        [TestMethod]
        public async Task DispatchAsync_GroupsByChannelType_InvokesSendersForEachUser()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();

            using var ctx = CreateContext(conn);

            // Seed users
            var alice = new User { Id = Guid.NewGuid(), Email = "alice@example.com", DisplayName = "Alice", CreatedAt = DateTimeOffset.UtcNow };
            var bob = new User { Id = Guid.NewGuid(), Email = "bob@example.com", DisplayName = "Bob", CreatedAt = DateTimeOffset.UtcNow };
            ctx.Users.AddRange(alice, bob);

            // Seed channels
            var emailChannel = new Channel { Id = Guid.NewGuid(), Type = "email", ConfigJson = "{\"from\":\"no-reply@example.com\"}", CreatedAt = DateTimeOffset.UtcNow };
            var slackChannel = new Channel { Id = Guid.NewGuid(), Type = "slack", ConfigJson = "{\"webhookUrl\":\"https://hooks.test/abc\"}", CreatedAt = DateTimeOffset.UtcNow };
            ctx.Channels.AddRange(emailChannel, slackChannel);

            // Subscriptions: Alice -> email, Bob -> email + slack
            ctx.UsersChannels.AddRange(
                new UsersChannels { UserId = alice.Id, ChannelId = emailChannel.Id },
                new UsersChannels { UserId = bob.Id, ChannelId = emailChannel.Id },
                new UsersChannels { UserId = bob.Id, ChannelId = slackChannel.Id }
            );

            ctx.SaveChanges();

            // Prepare fake factory that returns FakeSender instances per type
            var fakeFactory = new FakeSenderFactory();

            var logger = LoggerFactory.Create(b => { }).CreateLogger<NotificationDispatcher>();
            var dispatcher = new NotificationDispatcher(ctx, fakeFactory, logger);

            var message = new OutgoingMessage { Title = "Hi", Body = "Test" };

            await dispatcher.DispatchAsync(message);

            // Assert: email sender called for Alice and Bob, slack sender called for Bob only
            Assert.IsTrue(fakeFactory.Senders.ContainsKey("email"));
            Assert.IsTrue(fakeFactory.Senders.ContainsKey("slack"));

            var emailCalls = fakeFactory.Senders["email"].Calls;
            var slackCalls = fakeFactory.Senders["slack"].Calls;

            CollectionAssert.AreEquivalent(new List<Guid> { alice.Id, bob.Id }, emailCalls.Select(c => c.User.Id).ToList());
            CollectionAssert.AreEquivalent(new List<Guid> { bob.Id }, slackCalls.Select(c => c.User.Id).ToList());
        }

        [TestMethod]
        public async Task DispatchAsync_NoSubscriptions_NoSendersCalled()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();

            using var ctx = CreateContext(conn);

            // No seed

            var fakeFactory = new FakeSenderFactory();
            var logger = LoggerFactory.Create(b => { }).CreateLogger<NotificationDispatcher>();
            var dispatcher = new NotificationDispatcher(ctx, fakeFactory, logger);

            var message = new OutgoingMessage { Title = "Empty", Body = "Nobody" };
            await dispatcher.DispatchAsync(message);

            Assert.AreEqual(0, fakeFactory.Senders.Count);
        }

        // Helper fake sender and factory
        private class FakeSender : INotificationSender
        {
            public List<(OutgoingMessage Message, User User)> Calls { get; } = new List<(OutgoingMessage, User)>();

            public Task<DeliveryResult> SendAsync(OutgoingMessage message, User user, CancellationToken ct = default)
            {
                Calls.Add((message, user));
                return Task.FromResult(new DeliveryResult { Success = true, Status = "Sent" });
            }
        }

        private class FakeSenderFactory : INotificationSenderFactory
        {
            public readonly Dictionary<string, FakeSender> Senders = new Dictionary<string, FakeSender>(StringComparer.OrdinalIgnoreCase);

            public INotificationSender? GetSender(string channelType)
            {
                if (string.IsNullOrEmpty(channelType)) return null;
                if (!Senders.TryGetValue(channelType, out var sender))
                {
                    sender = new FakeSender();
                    Senders[channelType] = sender;
                }
                return sender;
            }
        }
    }
}
