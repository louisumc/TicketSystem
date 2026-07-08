using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
    public class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> builder)
        {
            builder.ToTable("Trips");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Origin)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Destination)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.DepartureTime)
                .IsRequired();

            builder.Property(t => t.ArrivalTime)
                .IsRequired();

            builder.Property(t => t.BusId)
                .IsRequired();

            builder.Property(t => t.Price)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(t => t.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(t => t.BusId)
                .HasDatabaseName("IX_Trips_BusId");

            builder.HasIndex(t => new { t.Origin, t.Destination })
                .HasDatabaseName("IX_Trips_Origin_Destination");

            builder.HasIndex(t => t.DepartureTime)
                .HasDatabaseName("IX_Trips_DepartureTime");

            builder.HasIndex(t => t.Status)
                .HasDatabaseName("IX_Trips_Status");

            builder.HasOne(t => t.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
