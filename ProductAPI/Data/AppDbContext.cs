using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace ProductAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, CategoryName = "Electronics" },
                new Category { Id = 2, CategoryName = "Clothing" },
                new Category { Id = 3, CategoryName = "Books" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Wireless Mouse",
                    Description = "2.4GHz wireless optical mouse",
                    ImageUrl = "https://example.com/images/wireless-mouse.jpg",
                    Price = 19.99m,
                    StockQuantity = 150,
                    CategoryId = 1,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 2,
                    Name = "Mechanical Keyboard",
                    Description = "Full-size keyboard with blue switches",
                    ImageUrl = "https://example.com/images/mechanical-keyboard.jpg",
                    Price = 79.50m,
                    StockQuantity = 60,
                    CategoryId = 1,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 3,
                    Name = "Cotton T-Shirt",
                    Description = "100% cotton crew neck t-shirt",
                    ImageUrl = "https://example.com/images/cotton-tshirt.jpg",
                    Price = 14.00m,
                    StockQuantity = 200,
                    CategoryId = 2,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 4,
                    Name = "Clean Code",
                    Description = "A handbook of agile software craftsmanship",
                    ImageUrl = "https://example.com/images/clean-code.jpg",
                    Price = 32.00m,
                    StockQuantity = 40,
                    CategoryId = 3,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
