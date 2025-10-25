using Domain_Layer.Models;
using Domain_Layer.Models; // Your entities like Customer, XeroToken, etc.
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Infrastructure_Layer.Data
{
    public class AccountingDbContext : DbContext
    {
        public AccountingDbContext(DbContextOptions<AccountingDbContext> options)
            : base(options) { }
        //parameterless constructor for design-time tools
        public AccountingDbContext()
        {
        }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<XeroTokenResponse> XeroTokenResponse { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer PK
            modelBuilder.Entity<Customer>()
                .HasKey(c => c.Id);

            // XeroToken PK
            modelBuilder.Entity<XeroTokenResponse>()
                .HasKey(t => t.Id);

            // Invoice PK
            modelBuilder.Entity<Invoice>()
                .HasKey(i => i.Id);

            // Quote PK
            modelBuilder.Entity<Quote>()
                .HasKey(q => q.Id);

            // ✅ Proper foreign key relationship for Invoice → Customer
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany()                   // You can replace WithMany(c => c.Invoices) if you add ICollection<Invoice> in Customer
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // ✅ Proper foreign key relationship for Quote → Customer
            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Customer)
                .WithMany()
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Precision for amounts
            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Quote>()
                .Property(q => q.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // ✅ Prevent duplicate customers
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.Name, c.Email, c.Phone, c.Address })
                .IsUnique();
        }
    }
}
