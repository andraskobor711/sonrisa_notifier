using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Api.Controllers;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System;
using System.Linq;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class UsersChannelsControllerTests
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
        public void UsersChannels_Endpoints_Work()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var ctx = CreateContext(connection);
            var userRepo = new UserRepository(ctx);
            var channelRepo = new ChannelRepository(ctx);
            var ucRepo = new UsersChannelsRepository(ctx);

            var controller = new UsersChannelsController(ucRepo, userRepo);

            var user = new User { Id = Guid.NewGuid(), Email = "ucuser2@example.com", CreatedAt = DateTimeOffset.UtcNow, IsActive = true };
            var channel = new Channel { Id = Guid.NewGuid(), Type = "slack", CreatedAt = DateTimeOffset.UtcNow, IsActive = true };

            ctx.Users.Add(user);
            ctx.Channels.Add(channel);
            ctx.SaveChanges();

            // Add mapping
            var addRes = controller.Add(new UsersChannels { UserId = user.Id, ChannelId = channel.Id }).Result as Microsoft.AspNetCore.Mvc.CreatedResult;
            Assert.IsNotNull(addRes);

            // Get all mappings
            var allRes = controller.GetAll().Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(allRes);
            var all = allRes.Value as System.Collections.Generic.List<UsersChannels>;
            Assert.IsTrue(all.Any(m => m.UserId == user.Id && m.ChannelId == channel.Id));

            // Get channels for user
            var chRes = controller.GetByUser(user.Id).Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(chRes);
            var channels = chRes.Value as System.Collections.Generic.List<Channel>;
            Assert.IsTrue(channels.Any(c => c.Id == channel.Id));

            // Get users for channel
            var uRes = controller.GetByChannel(channel.Id).Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(uRes);
            var users = uRes.Value as System.Collections.Generic.List<User>;
            Assert.IsTrue(users.Any(u => u.Id == user.Id));

            // Remove mapping
            var rem = controller.Remove(user.Id, channel.Id).Result as Microsoft.AspNetCore.Mvc.StatusCodeResult;
            Assert.IsNotNull(rem);
            Assert.AreEqual(204, rem.StatusCode);
        }
    }
}
