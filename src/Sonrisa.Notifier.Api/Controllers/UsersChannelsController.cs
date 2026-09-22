using Microsoft.AspNetCore.Mvc;
using Sonrisa.Notifier.Infrastructure.Entities;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Api.Controllers
{
    // NOTE: In production code this controller would be a thin HTTP adapter that calls into
    // a Core-level manager/service (e.g., IUsersChannelsManager) which implements business rules
    // and coordinates repositories. Also prefer returning DTOs rather than EF entities to avoid
    // leaking persistence details into API contracts.
    [ApiController]
    [Route("api/[controller]")]
    public class UsersChannelsController : ControllerBase
    {
        private readonly IUsersChannelsRepository _repo;
        private readonly IUserRepository _userRepo;

        public UsersChannelsController(IUsersChannelsRepository repo, IUserRepository userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = await _repo.GetAllAsync();
            return Ok(all);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var channels = await _repo.GetChannelsForUserAsync(userId);
            return Ok(channels);
        }

        [HttpGet("channel/{channelId}")]
        public async Task<IActionResult> GetByChannel(Guid channelId)
        {
            var userIds = await _repo.GetUserIdsForChannelAsync(channelId);
            var users = new List<User>();
            foreach (var id in userIds)
            {
                var u = await _userRepo.GetByIdAsync(id);
                if (u != null) users.Add(u);
            }
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] UsersChannels mapping)
        {
            await _repo.AddAsync(mapping);
            return Created(string.Empty, mapping);
        }

        [HttpDelete("{userId}/{channelId}")]
        public async Task<IActionResult> Remove(Guid userId, Guid channelId)
        {
            await _repo.RemoveAsync(userId, channelId);
            return NoContent();
        }
    }
}
