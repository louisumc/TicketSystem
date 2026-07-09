using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
    public class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
    {
        public void Configure(EntityTypeBuilder<Passenger> builder)
        {
            builder.ToTable("Passengers");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

            builder.Property(p => p.Document)
            .IsRequired()
            .HasMaxLength(20);

            builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(100);

            builder.Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(20);

            builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

            builder.HasIndex(p => p.Document)
            .IsUnique()
            .HasDatabaseName("IX_Passengers_Document");

            builder.HasIndex(p => p.Email)
            .HasDatabaseName("IX_Passengers_Email");
        }
    }
}