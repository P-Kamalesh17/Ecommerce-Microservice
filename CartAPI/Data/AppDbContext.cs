using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CartAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cart>(cart =>
            {
                cart.Property(c => c.UserId).HasMaxLength(450);
                cart.HasIndex(c => c.UserId).IsUnique();
            });

            modelBuilder.Entity<CartItem>(item =>
            {
                item.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                item.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();
            });
        }
    }
}
