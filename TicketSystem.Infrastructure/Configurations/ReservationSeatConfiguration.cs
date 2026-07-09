using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
    public class ReservationSeatConfiguration : IEntityTypeConfiguration<ReservationSeat>
    {
        public void Configure(EntityTypeBuilder<ReservationSeat> builder)
        {
            builder.ToTable("ReservationSeats");

            builder.HasKey(rs => rs.Id);

            builder.Property(rs => rs.Price)
            .IsRequired()
            .HasPrecision(10, 2);

            builder.Property(rs => rs.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");

            builder.Property(rs => rs.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

            builder.HasIndex(rs => rs.ReservationId)
            .HasDatabaseName("IX_ReservationSeats_ReservationId");

            builder.HasIndex(rs => rs.SeatId)
            .IsUnique()
            .HasDatabaseName("IX_ReservationSeats_SeatId");

            builder.HasOne(rs => rs.Reservation)
            .WithMany(r => r.ReservationSeats)
            .HasForeignKey(rs => rs.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rs => rs.Seat)
            .WithMany(s => s.ReservationSeats)
            .HasForeignKey(rs => rs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}