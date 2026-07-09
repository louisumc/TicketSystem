using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Data
{
public class ApplicationDbContext : DbContext
{
public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: base(options)
{
}

public DbSet<Bus> Buses { get; set; }
public DbSet<Trip> Trips { get; set; }
public DbSet<Seat> Seats { get; set; }
public DbSet<Passenger> Passengers { get; set; }
public DbSet<Reservation> Reservations { get; set; }
public DbSet<ReservationSeat> ReservationSeats { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
base.OnModelCreating(modelBuilder);
modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}

public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
var entries = ChangeTracker.Entries<BaseEntity>();

foreach (var entry in entries)
{
if (entry.State == EntityState.Modified)
{
entry.Entity.UpdatedAt = DateTime.UtcNow;
}
}

return await base.SaveChangesAsync(cancellationToken);
}
}
}