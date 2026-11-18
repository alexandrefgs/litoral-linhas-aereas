using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Litoral.Infrastructure.Reference;

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
    public DbSet<AirportCatalogEntry> AirportCatalog => Set<AirportCatalogEntry>();
    public DbSet<ScoringRule> ScoringRules => Set<ScoringRule>();
    public DbSet<MonthlyRoster> MonthlyRosters => Set<MonthlyRoster>();
    public DbSet<RosterLeg> RosterLegs => Set<RosterLeg>();
    public DbSet<FlightSession> FlightSessions => Set<FlightSession>();
    public DbSet<FlightTrackPoint> FlightTrackPoints => Set<FlightTrackPoint>();
    public DbSet<Pirep> Pireps => Set<Pirep>();
    public DbSet<SpecialFlight> SpecialFlights => Set<SpecialFlight>();
    public DbSet<SpecialFlightLeg> SpecialFlightLegs => Set<SpecialFlightLeg>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MonthlyAward> MonthlyAwards => Set<MonthlyAward>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

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