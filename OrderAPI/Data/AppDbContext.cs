using Microsoft.EntityFrameworkCore;
using OrderAPI.Model;

namespace OrderAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(order =>
            {
                order.HasIndex(o => o.UserId);
                order.HasIndex(o => o.Status);
                order.Property(o => o.Status).HasConversion<int>();
            });

            modelBuilder.Entity<OrderItem>(item =>
            {
                item.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
