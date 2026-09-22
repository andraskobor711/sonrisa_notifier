using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Infrastructure.Repositories
{
    public class UsersChannelsRepository : IUsersChannelsRepository
    {
        private readonly SonrisaNotifierDbContext _db;

        public UsersChannelsRepository(SonrisaNotifierDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(UsersChannels usersChannel)
        {
            _db.UsersChannels.Add(usersChannel);
            await _db.SaveChangesAsync();
        }

        public async Task<List<UsersChannels>> GetAllAsync()
        {
            return await _db.UsersChannels.ToListAsync();
        }

        public async Task RemoveAsync(Guid userId, Guid channelId)
        {
            var uc = await _db.UsersChannels.FindAsync(userId, channelId);
            if (uc != null)
            {
                _db.UsersChannels.Remove(uc);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Guid>> GetChannelIdsForUserAsync(Guid userId)
        {
            return await _db.UsersChannels
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.ChannelId)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetUserIdsForChannelAsync(Guid channelId)
        {
            return await _db.UsersChannels
                .Where(uc => uc.ChannelId == channelId)
                .Select(uc => uc.UserId)
                .ToListAsync();
        }

        public async Task<List<Channel>> GetChannelsForUserAsync(Guid userId)
        {
            var ids = await GetChannelIdsForUserAsync(userId);
            return await _db.Channels.Where(c => ids.Contains(c.Id)).ToListAsync();
        }
    }
}
