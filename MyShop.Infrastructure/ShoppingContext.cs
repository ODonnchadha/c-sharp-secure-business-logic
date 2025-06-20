using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Models;
using System.Reflection.Metadata;

namespace MyShop.Infrastructure
{
    public class ShoppingContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // !!WARNING!!
            // NEVER store the connection string directly in your code/repository
            // ALWAYS ensure it is stored securely
            var connectionString =
                "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                "Initial Catalog=MyShop;" +
                "Integrated Security=True;";

            optionsBuilder
                .UseSqlServer(connectionString)
                .UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();
        }
    }
}