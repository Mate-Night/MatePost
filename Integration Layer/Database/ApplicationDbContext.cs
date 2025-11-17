using BusinessLogicLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IntegrationLayer.Database
{
    /// <summary>
    /// Контекст бази даних для Entity Framework
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Client> Clients { get; set; }
        public DbSet<Parcel> Parcels { get; set; }
        public DbSet<Operator> Operators { get; set; }
        public DbSet<DeliveryPoint> DeliveryPoints { get; set; }
        public DbSet<StatusChange> StatusChanges { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування таблиці Client
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            // Налаштування таблиці Parcel
            modelBuilder.Entity<Parcel>(entity =>
            {
                entity.HasKey(e => e.TrackingNumber);
                entity.Property(e => e.TrackingNumber).HasMaxLength(50);

                // Зв'язки
                entity.HasOne<Client>()
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Client>()
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Колекції
                entity.HasMany(p => p.StatusHistory)
                    .WithOne()
                    .HasForeignKey("ParcelTrackingNumber")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Notifications)
                    .WithOne()
                    .HasForeignKey("ParcelTrackingNumber")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Налаштування таблиці Operator
            modelBuilder.Entity<Operator>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            });

            // Налаштування таблиці DeliveryPoint
            modelBuilder.Entity<DeliveryPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.OrganizationName).HasMaxLength(200);
            });

            // Налаштування таблиці StatusChange
            modelBuilder.Entity<StatusChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Note).HasMaxLength(1000);
            });

            // Налаштування таблиці Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Note).HasMaxLength(1000);
            });
        }
    }
}