using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Persistence;

public sealed class LitoralDbContext(DbContextOptions<LitoralDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Hub> Hubs => Set<Hub>();
    public DbSet<Rank> Ranks => Set<Rank>();
    public DbSet<AircraftType> AircraftTypes => Set<AircraftType>();
    public DbSet<Pilot> Pilots => Set<Pilot>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LitoralDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
        }
    }
}