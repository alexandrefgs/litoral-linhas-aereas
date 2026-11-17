using Litoral.Application.Flights;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class CrewCards(LitoralDbContext db)
{
    public const string VirtualCrewName = "Severino";

    public async Task<(FlightCrewMemberDto Captain, FlightCrewMemberDto FirstOfficer)> BuildAsync(Pilot pilot, Pilot? crew, CancellationToken ct)
    {
        var ranks = await db.Ranks.AsNoTracking().OrderBy(r => r.Level).ToListAsync(ct);

        var pilotIsCaptain = crew is null
            ? pilot.Rank.Level == ranks[^1].Level
            : pilot.Rank.Level >= crew.Rank.Level;

        var pilotDto = await MemberAsync(pilot, ct);
        var crewDto = crew is not null
            ? await MemberAsync(crew, ct)
            : Virtual(pilotIsCaptain ? ranks[0] : ranks[^1]);

        return pilotIsCaptain ? (pilotDto, crewDto) : (crewDto, pilotDto);
    }

    private async Task<FlightCrewMemberDto> MemberAsync(Pilot pilot, CancellationToken ct)
    {
        var flights = await db.Pireps.CountAsync(p => p.PilotId == pilot.Id && p.Status != PirepStatus.Pending, ct);
        return new FlightCrewMemberDto(
            pilot.PilotNumber,
            pilot.DisplayId,
            pilot.PublicName,
            pilot.Rank.Code,
            pilot.Rank.Name,
            pilot.PhotoUrl,
            flights,
            pilot.TotalFlightMinutes,
            pilot.Points,
            false);
    }

    private static FlightCrewMemberDto Virtual(Rank rank) =>
        new(null, null, VirtualCrewName, rank.Code, rank.Name, null, 0, 0, 0, true);
}