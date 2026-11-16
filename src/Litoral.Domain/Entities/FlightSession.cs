using Litoral.Domain.Common;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;

namespace Litoral.Domain.Entities;

public sealed class FlightSession : Entity
{
    private readonly List<FlightTrackPoint> _points = [];

    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public Guid PilotId { get; private set; }
    public FlightNetwork Network { get; private set; }
    public string? Simulator { get; private set; }
    public string? Client { get; private set; }
    public FlightSessionStatus Status { get; private set; } = FlightSessionStatus.Active;
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public int? AltitudeFt { get; private set; }
    public int? GroundSpeedKt { get; private set; }
    public int? Heading { get; private set; }
    public string? Phase { get; private set; }
    public DateTime? LastReportAt { get; private set; }

    public IReadOnlyCollection<FlightTrackPoint> Points => _points;

    public bool IsActive => Status == FlightSessionStatus.Active;

    private FlightSession() { }

    public FlightSession(Booking booking, FlightNetwork network, string? simulator, string? client)
    {
        if (booking.Status == BookingStatus.Booked)
            booking.Start();
        else if (booking.Status != BookingStatus.InProgress)
            throw new DomainException("This booking can't be flown.");

        Booking = booking;
        BookingId = booking.Id;
        PilotId = booking.PilotId;
        Network = network;
        Simulator = Trim(simulator, 60);
        Client = Trim(client, 60);
    }

    public int AddPositions(IEnumerable<TelemetrySample> samples)
    {
        if (!IsActive) throw new DomainException("This flight is no longer active.");

        var added = 0;

        foreach (var sample in samples.OrderBy(s => s.At))
        {
            if (LastReportAt is DateTime last && sample.At <= last) continue;
            if (sample.Latitude is < -90 or > 90 || sample.Longitude is < -180 or > 180) continue;

            _points.Add(new FlightTrackPoint(this, sample));

            Latitude = sample.Latitude;
            Longitude = sample.Longitude;
            AltitudeFt = sample.AltitudeFt;
            GroundSpeedKt = sample.GroundSpeedKt;
            Heading = sample.Heading;
            Phase = Trim(sample.Phase, 30);
            LastReportAt = sample.At;
            added++;
        }

        return added;
    }

    public void Complete()
    {
        if (!IsActive) throw new DomainException("This flight is no longer active.");
        Status = FlightSessionStatus.Completed;
        EndedAt = DateTime.UtcNow;
    }

    public void Abandon()
    {
        if (!IsActive) return;
        Status = FlightSessionStatus.Abandoned;
        EndedAt = DateTime.UtcNow;
    }

    internal static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length > max ? value[..max] : value;
    }
}

public sealed class FlightTrackPoint
{
    public long Id { get; private set; }
    public Guid SessionId { get; private set; }
    public DateTime At { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int AltitudeFt { get; private set; }
    public int? AglFt { get; private set; }
    public int? IasKt { get; private set; }
    public int GroundSpeedKt { get; private set; }
    public int Heading { get; private set; }
    public int? VerticalSpeedFpm { get; private set; }
    public int? Flaps { get; private set; }
    public bool? GearDown { get; private set; }
    public bool? OnGround { get; private set; }
    public string? Phase { get; private set; }

    private FlightTrackPoint() { }

    internal FlightTrackPoint(FlightSession session, TelemetrySample s)
    {
        SessionId = session.Id;
        At = s.At;
        Latitude = s.Latitude;
        Longitude = s.Longitude;
        AltitudeFt = s.AltitudeFt;
        AglFt = s.AglFt;
        IasKt = s.IasKt;
        GroundSpeedKt = s.GroundSpeedKt;
        Heading = s.Heading;
        VerticalSpeedFpm = s.VerticalSpeedFpm;
        Flaps = s.Flaps;
        GearDown = s.GearDown;
        OnGround = s.OnGround;
        Phase = FlightSession.Trim(s.Phase, 30);
    }

    public TelemetrySample ToSample() =>
        new(At, Latitude, Longitude, AltitudeFt, AglFt, IasKt, GroundSpeedKt, Heading, VerticalSpeedFpm, Flaps, GearDown, OnGround, Phase);
}