using Microsoft.AspNetCore.Mvc;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Api.Controllers
{
    // NOTE: Prefer introducing a Channels manager/service in the Core layer (e.g., IChannelManager)
    // and DTOs for the API surface. The controller should call that manager instead of
    // directly using repository methods and entity objects. This improves testability and
    // separates HTTP concerns from business logic.
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelsController : ControllerBase
    {
        private readonly IChannelRepository _repo;

        public ChannelsController(IChannelRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var channels = await _repo.GetAllAsync();
            return Ok(channels);
        }
    }
}
