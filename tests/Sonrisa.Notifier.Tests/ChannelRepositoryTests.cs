using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class ChannelRepositoryTests
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
        public async Task ChannelRepository_CRUD_Works()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            await using var ctx = CreateContext(connection);
            var repo = new ChannelRepository(ctx);

            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                Type = "email",
                ConfigJson = "{\"from\":\"no-reply@example.com\"}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await repo.AddAsync(channel);

            var fetched = await repo.GetByIdAsync(channel.Id);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(channel.Type, fetched!.Type);

            fetched.ConfigJson = "{\"from\":\"support@example.com\"}";
            await repo.UpdateAsync(fetched);

            var updated = await repo.GetByIdAsync(channel.Id);
            Assert.AreEqual("{\"from\":\"support@example.com\"}", updated!.ConfigJson);

            await repo.DeleteAsync(channel.Id);
            var deleted = await repo.GetByIdAsync(channel.Id);
            Assert.IsNull(deleted);
        }
    }
}
