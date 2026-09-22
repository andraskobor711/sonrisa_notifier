using Sonrisa.Notifier.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sonrisa.Notifier.Infrastructure.Repositories
{
    public interface IChannelRepository
    {
        Task<Channel?> GetByIdAsync(Guid id);
        Task<List<Channel>> GetAllAsync();
        Task AddAsync(Channel channel);
        Task UpdateAsync(Channel channel);
        Task DeleteAsync(Guid id);
    }
}
