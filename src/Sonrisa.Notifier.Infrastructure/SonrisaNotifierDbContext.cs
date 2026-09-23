using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure.Entities;

namespace Sonrisa.Notifier.Infrastructure
{
    public class SonrisaNotifierDbContext : DbContext
    {
        public SonrisaNotifierDbContext(DbContextOptions<SonrisaNotifierDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<UsersChannels> UsersChannels { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsersChannels>()
                .HasKey(uc => new { uc.UserId, uc.ChannelId });

            // Configure relationships with cascade delete so removing a User or Channel
            // will remove corresponding UsersChannels entries and avoid FK constraint failures.
            modelBuilder.Entity<UsersChannels>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsersChannels>()
                .HasOne<Channel>()
                .WithMany()
                .HasForeignKey(uc => uc.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure simple mappings; further configuration can be added later
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();

            modelBuilder.Entity<Channel>()
                .Property(c => c.Type)
                .IsRequired();
        }
    }
}
