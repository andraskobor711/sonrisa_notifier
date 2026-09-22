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
    public class ChannelsControllerTests
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
        public void GetAllChannels_Works()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var ctx = CreateContext(connection);
            var repo = new ChannelRepository(ctx);
            var controller = new ChannelsController(repo);

            var ch = new Channel { Id = Guid.NewGuid(), Type = "email", ConfigJson = "{}", CreatedAt = DateTimeOffset.UtcNow };
            ctx.Channels.Add(ch);
            ctx.SaveChanges();

            var res = controller.GetAll().Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(res);
            var list = res.Value as System.Collections.Generic.List<Channel>;
            Assert.IsTrue(list.Any(c => c.Id == ch.Id));
        }
    }
}
