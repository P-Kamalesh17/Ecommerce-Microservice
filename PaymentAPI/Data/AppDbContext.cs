using Microsoft.EntityFrameworkCore;
using PaymentAPI.Model;

namespace PaymentAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(payment =>
            {
                payment.HasIndex(p => p.OrderId).IsUnique();
                payment.HasIndex(p => p.UserId);
                payment.Property(p => p.Status).HasConversion<int>();
            });
        }
    }
}
