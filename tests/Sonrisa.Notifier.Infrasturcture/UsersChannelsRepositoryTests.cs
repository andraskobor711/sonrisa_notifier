using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class UsersChannelsRepositoryTests
    {
        private SonrisaNotifierDbContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<SonrisaNotifierDbContext>()
                .UseSqlite(connection)
                .Options;

            var ctx = new SonrisaNotifierDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        [TestMethod]
        public async Task UsersChannels_Add_Get_Remove_Works()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            await using var ctx = CreateContext(connection);
            var ucRepo = new UsersChannelsRepository(ctx);

            var user = new User { Id = Guid.NewGuid(), Email = "ucuser@example.com", CreatedAt = DateTimeOffset.UtcNow, IsActive = true };
            var channel = new Channel { Id = Guid.NewGuid(), Type = "email", CreatedAt = DateTimeOffset.UtcNow, IsActive = true };

            ctx.Users.Add(user);
            ctx.Channels.Add(channel);
            ctx.SaveChanges();

            await ucRepo.AddAsync(new UsersChannels { UserId = user.Id, ChannelId = channel.Id });

            var channelIds = await ucRepo.GetChannelIdsForUserAsync(user.Id);
            Assert.AreEqual(1, channelIds.Count);
            Assert.AreEqual(channel.Id, channelIds[0]);

            var channels = await ucRepo.GetChannelsForUserAsync(user.Id);
            Assert.AreEqual(1, channels.Count);
            Assert.AreEqual(channel.Id, channels[0].Id);

            await ucRepo.RemoveAsync(user.Id, channel.Id);
            var channelIdsAfter = await ucRepo.GetChannelIdsForUserAsync(user.Id);
            Assert.AreEqual(0, channelIdsAfter.Count);
        }
    }
}
