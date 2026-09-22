using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Api.Controllers;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class TestControllerTests
    {
        private class FakeDispatcher : INotificationDispatcher
        {
            public OutgoingMessage? ReceivedMessage { get; private set; }
            public Task DispatchAsync(OutgoingMessage message, CancellationToken ct = default)
            {
                ReceivedMessage = message;
                return Task.CompletedTask;
            }
        }

        [TestMethod]
        public async Task Post_ValidMessage_CallsDispatcherAndReturnsAccepted()
        {
            var fake = new FakeDispatcher();
            var controller = new TestController(fake);

            var msg = new OutgoingMessage { Title = "Hello", Body = "World" };

            var result = await controller.Post(msg);

            Assert.IsNotNull(fake.ReceivedMessage);
            Assert.AreEqual("Hello", fake.ReceivedMessage.Title);
            Assert.AreEqual("World", fake.ReceivedMessage.Body);

            var accepted = result as Microsoft.AspNetCore.Mvc.AcceptedResult;
            Assert.IsNotNull(accepted);
        }

        [TestMethod]
        public async Task Post_MissingTitleOrBody_ReturnsBadRequest()
        {
            var fake = new FakeDispatcher();
            var controller = new TestController(fake);

            var msg1 = new OutgoingMessage { Title = "", Body = "Body" };
            var res1 = await controller.Post(msg1) as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
            Assert.IsNotNull(res1);

            var msg2 = new OutgoingMessage { Title = "Title", Body = "" };
            var res2 = await controller.Post(msg2) as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
            Assert.IsNotNull(res2);
        }
    }
}
