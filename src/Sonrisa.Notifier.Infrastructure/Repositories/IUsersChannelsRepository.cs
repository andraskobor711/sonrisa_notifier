using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Infrastructure.Repositories
{
    public interface IUsersChannelsRepository
    {
        Task AddAsync(UsersChannels usersChannel);
        Task RemoveAsync(Guid userId, Guid channelId);
        Task<List<Guid>> GetChannelIdsForUserAsync(Guid userId);
        Task<List<Guid>> GetUserIdsForChannelAsync(Guid channelId);
        Task<List<Channel>> GetChannelsForUserAsync(Guid userId);
    }
}
