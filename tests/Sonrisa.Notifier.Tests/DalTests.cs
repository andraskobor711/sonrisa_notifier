using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Infrastructure;
using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Linq;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class DalTests
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
        public void Can_Create_Read_Update_Delete_User()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var ctx = CreateContext(connection);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com",
                DisplayName = "Test User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Create
            ctx.Users.Add(user);
            ctx.SaveChanges();

            // Read
            var fetched = ctx.Users.Single(u => u.Id == user.Id);
            Assert.AreEqual("testuser@example.com", fetched.Email);

            // Update
            fetched.DisplayName = "Updated Name";
            ctx.SaveChanges();

            var updated = ctx.Users.Single(u => u.Id == user.Id);
            Assert.AreEqual("Updated Name", updated.DisplayName);

            // Delete
            ctx.Users.Remove(updated);
            ctx.SaveChanges();

            Assert.IsFalse(ctx.Users.Any(u => u.Id == user.Id));
        }

        [TestMethod]
        public void Can_Create_Channels_And_UsersChannels_Mapping()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var ctx = CreateContext(connection);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "mappinguser@example.com",
                DisplayName = "Mapping User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                Type = "email",
                ConfigJson = "{\"from\":\"no-reply@example.com\"}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            ctx.Users.Add(user);
            ctx.Channels.Add(channel);
            ctx.UsersChannels.Add(new UsersChannels { UserId = user.Id, ChannelId = channel.Id });
            ctx.SaveChanges();

            var joined = (from u in ctx.Users
                          join uc in ctx.UsersChannels on u.Id equals uc.UserId
                          join c in ctx.Channels on uc.ChannelId equals c.Id
                          where u.Id == user.Id
                          select new { u.Email, ChannelType = c.Type }).ToList();

            Assert.AreEqual(1, joined.Count);
            Assert.AreEqual("email", joined[0].ChannelType);
        }
    }
}
