using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Configurations
{
public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
public void Configure(EntityTypeBuilder<Seat> builder)
{
builder.ToTable("Seats");

builder.HasKey(s => s.Id);

builder.Property(s => s.Number)
.IsRequired()
.HasMaxLength(10);

builder.Property(s => s.Type)
.IsRequired()
.HasConversion<int>();

builder.Property(s => s.Status)
.IsRequired()
.HasConversion<int>();

builder.Property(s => s.Row)
.IsRequired();

builder.Property(s => s.Column)
.IsRequired();

builder.Property(s => s.PassengerName)
.HasMaxLength(50);

builder.Property(s => s.PassengerDocument)
.HasMaxLength(20);

builder.Property(s => s.PriceMultiplier)
.HasPrecision(5, 2);

builder.Property(s => s.CreatedAt)
.IsRequired()
.HasDefaultValueSql("GETDATE()");

builder.Property(s => s.IsActive)
.IsRequired()
.HasDefaultValue(true);

builder.Property(s => s.RowVersion)
.IsRowVersion()
.IsConcurrencyToken();

builder.HasIndex(s => new { s.TripId, s.Number })
.IsUnique()
.HasDatabaseName("IX_Seats_TripId_Number");

builder.HasIndex(s => s.TripId)
.HasDatabaseName("IX_Seats_TripId");

builder.HasIndex(s => s.Status)
.HasDatabaseName("IX_Seats_Status");

builder.HasIndex(s => new { s.Row, s.Column })
.HasDatabaseName("IX_Seats_Row_Column");

builder.HasOne(s => s.Trip)
.WithMany(t => t.Seats)
.HasForeignKey(s => s.TripId)
.OnDelete(DeleteBehavior.Restrict);
}
}
}