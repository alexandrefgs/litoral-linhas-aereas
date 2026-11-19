using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Litoral.Infrastructure.Audit;

public sealed partial class AuditInterceptor(AuditContext audit) : SaveChangesInterceptor
{
    private const int MaxValueLength = 80;

    private static readonly HashSet<string> IgnoredProperties = ["Id", "CreatedAt"];

    private static readonly HashSet<string> PilotProperties =
    [
        nameof(Pilot.FirstName), nameof(Pilot.LastName), nameof(Pilot.BirthDate), nameof(Pilot.CountryCode),
        nameof(Pilot.State), nameof(Pilot.City), nameof(Pilot.VatsimId), nameof(Pilot.IvaoId), nameof(Pilot.DiscordId),
        nameof(Pilot.SimBriefUsername), nameof(Pilot.NameFormat), nameof(Pilot.PhotoUrl), nameof(Pilot.CoverUrl),
        nameof(Pilot.SimAircraftTypes), nameof(Pilot.IsActive), nameof(Pilot.BaseIcao), nameof(Pilot.PilotNumber)
    ];

    private static readonly HashSet<string> PirepProperties = [nameof(Pirep.Status), nameof(Pirep.ReviewNote)];
    
    private static readonly HashSet<string> DownloadProperties =
    [
        nameof(DownloadItem.Title), nameof(DownloadItem.Description), nameof(DownloadItem.Category), nameof(DownloadItem.AircraftType),
        nameof(DownloadItem.Version), nameof(DownloadItem.ExternalUrl), nameof(DownloadItem.FileName), nameof(DownloadItem.SizeBytes)
    ];

    private sealed record Subject(AuditCategory Category, string Type, string Name, IReadOnlySet<string>? Only = null);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is LitoralDbContext db && audit.InScope)
            await AddLogsAsync(db, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task AddLogsAsync(LitoralDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.DetectChanges();

        var entries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            if (Describe(entry) is not { } subject) continue;
            if (entry.Entity is Pirep && entry.State != EntityState.Modified) continue;
            if (entry.Entity is Pilot && entry.State == EntityState.Deleted) continue;

            var details = await DetailsAsync(db, entry, subject.Only, ct);
            if (entry.State == EntityState.Modified && details is null) continue;

            var (actorId, actorName) = await audit.ActorAsync(db, ct);
            var category = subject.Category;

            if (entry.Entity is Pilot pilot)
            {
                if (actorId is null)
                    (actorId, actorName) = (pilot.UserId, subject.Name);
                if (actorId == pilot.UserId)
                    category = AuditCategory.Account;
            }

            logs.Add(new AuditLog(
                category,
                ActionFor(entry, subject),
                actorId,
                actorName,
                subject.Type,
                (entry.Entity as Entity)?.Id.ToString(),
                subject.Name,
                details,
                audit.IpAddress));
        }

        if (logs.Count > 0)
            db.AuditLogs.AddRange(logs);
    }

    private static Subject? Describe(EntityEntry entry) => entry.Entity switch
    {
        Airport a => new(AuditCategory.Airports, "Airport", $"{a.Icao} · {a.Name}"),
        Route r => new(AuditCategory.Routes, "Route", r.Callsign),
        AircraftType t => new(AuditCategory.Fleet, "Aircraft type", $"{t.IcaoCode} · {t.Name}"),
        Aircraft a => new(AuditCategory.Fleet, "Aircraft", a.Registration),
        ScoringRule r => new(AuditCategory.Scoring, "Scoring rule", r.Description),
        SpecialFlight f => new(AuditCategory.Operations, "Special flight", f.Title),
        Award a => new(AuditCategory.Awards, "Award", a.Name),
        DownloadItem d => new(AuditCategory.Downloads, "Download", d.Title, DownloadProperties),
        Pirep p => new(AuditCategory.Pireps, "PIREP", p.Callsign, PirepProperties),
        Pilot p => new(AuditCategory.Pilots, "Pilot", AuditContext.PilotLabel(p), PilotProperties),
        _ => null
    };

    private static string ActionFor(EntityEntry entry, Subject subject)
    {
        if (entry.State == EntityState.Modified)
        {
            if (entry.Entity is Pilot && IsChanged(entry, nameof(Pilot.IsActive)))
                return entry.CurrentValues[nameof(Pilot.IsActive)] is true ? "Pilot reactivated" : "Pilot deactivated";
            if (entry.Entity is Pirep pirep && IsChanged(entry, nameof(Pirep.Status)))
                return $"PIREP {pirep.Status.ToString().ToLowerInvariant()}";
        }

        if (entry.Entity is Pilot && entry.State == EntityState.Added)
            return "Account created";

        return entry.State switch
        {
            EntityState.Added => $"{subject.Type} created",
            EntityState.Deleted => $"{subject.Type} deleted",
            _ => $"{subject.Type} updated"
        };
    }

    private static bool IsChanged(EntityEntry entry, string property)
    {
        var p = entry.Property(property);
        return p.IsModified && !Equals(p.OriginalValue, p.CurrentValue);
    }

    private static async Task<string?> DetailsAsync(LitoralDbContext db, EntityEntry entry, IReadOnlySet<string>? only, CancellationToken ct)
    {
        var lines = new List<string>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (property.Metadata.IsShadowProperty() || IgnoredProperties.Contains(name)) continue;
            if (only is not null && !only.Contains(name)) continue;

            switch (entry.State)
            {
                case EntityState.Modified:
                    if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue)) continue;
                    lines.Add($"{Label(name)}: {await FormatAsync(db, property, property.OriginalValue, ct)} → {await FormatAsync(db, property, property.CurrentValue, ct)}");
                    break;
                case EntityState.Added when property.CurrentValue is not null:
                    lines.Add($"{Label(name)}: {await FormatAsync(db, property, property.CurrentValue, ct)}");
                    break;
                case EntityState.Deleted when property.OriginalValue is not null:
                    lines.Add($"{Label(name)}: {await FormatAsync(db, property, property.OriginalValue, ct)}");
                    break;
            }
        }

        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    private static async Task<string> FormatAsync(LitoralDbContext db, PropertyEntry property, object? value, CancellationToken ct)
    {
        if (value is null) return "—";

        if (value is Guid id)
        {
            var foreignKey = property.Metadata.GetContainingForeignKeys().FirstOrDefault();
            if (foreignKey is not null && await db.FindAsync(foreignKey.PrincipalEntityType.ClrType, [id], ct) is { } principal)
                return NameOf(principal) ?? id.ToString();
            return id.ToString();
        }

        var text = value switch
        {
            DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "Z",
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bool b => b ? "Yes" : "No",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "—"
        };

        text = text.ReplaceLineEndings(" ");
        return text.Length > MaxValueLength ? text[..(MaxValueLength - 1)] + "…" : text;
    }

    private static string? NameOf(object entity) => entity switch
    {
        Airport a => a.Icao,
        Route r => r.Callsign,
        AircraftType t => t.IcaoCode,
        Aircraft a => a.Registration,
        Pilot p => AuditContext.PilotLabel(p),
        Rank r => r.Code,
        _ => null
    };

    private static string Label(string property)
    {
        if (property.Length > 2 && property.EndsWith("Id", StringComparison.Ordinal))
            property = property[..^2];

        var words = PascalCase().Replace(property, " $1").Trim();
        var builder = new StringBuilder(words.ToLowerInvariant());
        builder[0] = char.ToUpperInvariant(builder[0]);
        return builder.ToString();
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCase();
}