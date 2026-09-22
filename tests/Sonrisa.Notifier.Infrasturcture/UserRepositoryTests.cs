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
    public class RepositoryTests
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
        public async Task UserRepository_CRUD_Works()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            await using var ctx = CreateContext(connection);
            var repo = new UserRepository(ctx);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "repo@example.com",
                DisplayName = "Repo User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await repo.AddAsync(user);

            var fetched = await repo.GetByIdAsync(user.Id);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(user.Email, fetched!.Email);

            fetched.DisplayName = "Repo User Updated";
            await repo.UpdateAsync(fetched);

            var updated = await repo.GetByIdAsync(user.Id);
            Assert.AreEqual("Repo User Updated", updated!.DisplayName);

            await repo.DeleteAsync(user.Id);
            var deleted = await repo.GetByIdAsync(user.Id);
            Assert.IsNull(deleted);
        }
    }
}
