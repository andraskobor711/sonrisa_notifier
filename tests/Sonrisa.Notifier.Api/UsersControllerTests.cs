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
    public class UsersControllerTests
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
        public void Users_CRUD_Works()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var ctx = CreateContext(connection);
            var repo = new UserRepository(ctx);
            var controller = new UsersController(repo);

            var user = new User { Email = "test@example.com", DisplayName = "Tester" };

            // Create
            var createResult = controller.Create(user).Result as Microsoft.AspNetCore.Mvc.CreatedAtActionResult;
            Assert.IsNotNull(createResult);
            var created = createResult.Value as User;
            Assert.IsNotNull(created);
            Assert.AreNotEqual(Guid.Empty, created.Id);

            // GetAll
            var getAll = controller.GetAll().Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(getAll);
            var list = getAll.Value as System.Collections.Generic.List<User>;
            Assert.IsTrue(list.Any(u => u.Id == created.Id));

            // GetById
            var getBy = controller.GetById(created.Id).Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.IsNotNull(getBy);
            var fetched = getBy.Value as User;
            Assert.AreEqual(created.Id, fetched.Id);

            // Update
            created.DisplayName = "Updated";
            var updateResult = controller.Update(created.Id, created).Result as Microsoft.AspNetCore.Mvc.StatusCodeResult;
            Assert.IsNotNull(updateResult);
            Assert.AreEqual(204, updateResult.StatusCode);

            // Delete
            var deleteResult = controller.Delete(created.Id).Result as Microsoft.AspNetCore.Mvc.StatusCodeResult;
            Assert.IsNotNull(deleteResult);
            Assert.AreEqual(204, deleteResult.StatusCode);
        }
    }
}
