using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
    public class BusConfiguration : IEntityTypeConfiguration<Bus>
    {
        public void Configure(EntityTypeBuilder<Bus> builder)
        {
            builder.ToTable("Buses");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Plate)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(b => b.Model)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(b => b.Company)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.Capacity)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.Property(b => b.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(b => b.Plate)
                .IsUnique()
                .HasDatabaseName("IX_Buses_Plate");

            builder.HasIndex(b => b.Company)
                .HasDatabaseName("IX_Buses_Company");

            builder.HasMany(b => b.Trips)
                .WithOne(t => t.Bus)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}