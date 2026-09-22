using Microsoft.AspNetCore.Mvc;
using Sonrisa.Notifier.Infrastructure.Entities;
using Sonrisa.Notifier.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Api.Controllers
{
    // NOTE: In a real-world application the controller should NOT talk directly to repositories
    // and should not expose EF entity types. Instead:
    // - Define a Manager/Service interface in the Core project (e.g., IUserManager) that
    //   encapsulates business logic and orchestrates repositories/transactions.
    // - Use DTOs for request/response models and map between entities and DTOs (AutoMapper
    //   or manual mapping).
    // - Controllers should depend on the Manager/Service abstractions, not on repository
    //   implementations. This keeps the API layer thin and focused on HTTP concerns.
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _repo;

        public UsersController(IUserRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repo.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTimeOffset.UtcNow;
            await _repo.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] User updated)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Email = updated.Email;
            existing.DisplayName = updated.DisplayName;
            existing.Timezone = updated.Timezone;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            await _repo.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
