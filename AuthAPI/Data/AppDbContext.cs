using AuthAPI.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(user =>
            {
                user.Property(u => u.FullName).HasMaxLength(100);
                user.Property(u => u.Address).HasMaxLength(500);
            });

            modelBuilder.Entity<RefreshToken>(token =>
            {
                token.Property(t => t.TokenHash).HasMaxLength(64);
                token.HasIndex(t => t.TokenHash).IsUnique();
                token.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
