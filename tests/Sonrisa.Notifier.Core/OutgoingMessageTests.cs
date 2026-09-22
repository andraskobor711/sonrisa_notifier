using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonrisa.Notifier.Core.Models;

namespace Sonrisa.Notifier.Tests
{
    [TestClass]
    public class OutgoingMessageTests
    {
        [TestMethod]
        public void Constructor_SetsDefaults_AndProperties_AreAssignable()
        {
            // Arrange
            var userId = System.Guid.NewGuid();
            var title = "Test Title";
            var body = "Test Body";

            // Act
            var message = new OutgoingMessage
            {
                UserId = userId,
                Title = title,
                Body = body
            };

            // Assert
            Assert.AreEqual(userId, message.UserId);
            Assert.AreEqual(title, message.Title);
            Assert.AreEqual(body, message.Body);
            Assert.IsNotNull(message.MessageId);
            Assert.IsTrue(message.CreatedAt <= System.DateTimeOffset.UtcNow);
            CollectionAssert.AreEqual(new System.Collections.Generic.List<string>(), message.Channels);
        }
    }
}
