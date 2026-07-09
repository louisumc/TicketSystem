using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("Reservations");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReservationDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(r => r.ExpiresAt)
            .IsRequired();

            builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

            builder.Property(r => r.TotalAmount)
            .IsRequired()
            .HasPrecision(10, 2);

            builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

            builder.HasIndex(r => r.TripId)
            .HasDatabaseName("IX_Reservations_TripId");

            builder.HasIndex(r => r.PassengerId)
            .HasDatabaseName("IX_Reservations_PassengerId");

            builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Reservations_Status");

            builder.HasIndex(r => r.ExpiresAt)
            .HasDatabaseName("IX_Reservations_ExpiresAt");

            builder.HasOne(r => r.Trip)
            .WithMany()
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Passenger)
            .WithMany(p => p.Reservations)
            .HasForeignKey(r => r.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}