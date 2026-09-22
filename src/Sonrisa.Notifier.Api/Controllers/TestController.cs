using Microsoft.AspNetCore.Mvc;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Models;

namespace Sonrisa.Notifier.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly INotificationDispatcher _dispatcher;

        public TestController(INotificationDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        // POST api/test
        // Accepts a simple message payload and dispatches it to subscribers
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] OutgoingMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Title) || string.IsNullOrWhiteSpace(message.Body))
            {
                return BadRequest("Title and Body are required.");
            }

            await _dispatcher.DispatchAsync(message);
            return Accepted();
        }
    }
}
