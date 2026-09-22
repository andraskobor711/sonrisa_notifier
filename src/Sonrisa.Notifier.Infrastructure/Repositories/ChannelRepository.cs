using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Infrastructure.Repositories
{
    public class ChannelRepository : IChannelRepository
    {
        private readonly SonrisaNotifierDbContext _db;

        public ChannelRepository(SonrisaNotifierDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Channel channel)
        {
            _db.Channels.Add(channel);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var channel = await _db.Channels.FindAsync(id);
            if (channel != null)
            {
                _db.Channels.Remove(channel);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Channel>> GetAllAsync()
        {
            return await _db.Channels.ToListAsync();
        }

        public async Task<Channel?> GetByIdAsync(Guid id)
        {
            return await _db.Channels.FindAsync(id);
        }

        public async Task UpdateAsync(Channel channel)
        {
            _db.Channels.Update(channel);
            await _db.SaveChangesAsync();
        }
    }
}
