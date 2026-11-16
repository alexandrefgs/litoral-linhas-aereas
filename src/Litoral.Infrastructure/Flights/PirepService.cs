using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class PirepService(LitoralDbContext db, BookingSupport support, PirepFiling filing) : IPirepService
{
    private const int ListLimit = 100;
    private const int MinCommentsLength = 10;

    public async Task<IReadOnlyList<PirepSummaryDto>> ListMineAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        var pireps = await db.Pireps.AsNoTracking()
            .Where(p => p.PilotId == pilot.Id)
            .OrderByDescending(p => p.SubmittedAt)
            .Take(ListLimit)
            .ToListAsync(ct);

        return pireps.Select(PirepFiling.ToSummary).ToList();
    }

    public async Task<PirepDto> GetMineAsync(string userId, Guid id, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        var pirep = await filing.PirepQuery().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.PilotId == pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "PIREP not found.");

        return await filing.ToDtoAsync(pirep, ct);
    }

    public async Task<PirepDto> FileManualAsync(string userId, ManualPirepRequest request, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        if ((request.Comments ?? "").Trim().Length < MinCommentsLength)
            throw new AppException(AppErrorKind.Validation, "Explain in the comments why the flight is being filed manually.");

        var network = PirepFiling.ParseNetwork(request.Network);

        var session = await db.FlightSessions
            .FirstOrDefaultAsync(s => s.BookingId == booking.Id && s.Status == FlightSessionStatus.Active, ct);
        session?.Abandon();

        var report = new PirepReport(
            PirepFiling.Utc(request.BlockOffAt),
            PirepFiling.Utc(request.BlockOnAt),
            PirepFiling.Utc(request.TakeoffAt),
            PirepFiling.Utc(request.LandingAt),
            request.LandingRateFpm is int rate ? -Math.Abs(rate) : null,
            request.FuelUsedKg,
            null,
            null,
            request.FlightRoute,
            null,
            null,
            request.Comments);

        var data = new FlightEvaluationData
        {
            AircraftTypeIcao = booking.Aircraft.AircraftType.IcaoCode,
            PlannedArrivalIcao = booking.Route.ArrivalAirport.Icao,
            ArrivalIcao = request.ArrivalIcao,
            Network = network,
            LandingRateFpm = report.LandingRateFpm,
            DiversionReported = true
        };

        var pirep = await filing.FileAsync(booking, null, PirepSource.Manual, network, request.ArrivalIcao, report, data, [], ct);
        return await filing.ToDtoAsync(pirep, ct);
    }
}