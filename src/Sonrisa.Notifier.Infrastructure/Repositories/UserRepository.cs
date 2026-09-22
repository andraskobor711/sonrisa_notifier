using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SonrisaNotifierDbContext _db;

        public UserRepository(SonrisaNotifierDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _db.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }
    }
}
