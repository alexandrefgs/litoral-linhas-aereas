using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Core.Flight;

public sealed class FlightRecorder
{
    public static readonly TimeSpan DataGapLimit = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan CruiseHold = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan GateInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan GroundInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan LowAltitudeInterval = TimeSpan.FromSeconds(1);

    public const int LowAltitudeAglFt = 2500;
    public const int ApproachAglFt = 3000;
    public const int ClimbDescentFpm = 300;
    public const int LevelBandFt = 300;
    public const int TakeoffRollKt = 40;
    public const int LiftoffMinIasKt = 60;
    public const double RotationPitchDeg = 2;
    public const int TaxiInBelowKt = 40;
    public const double RefuelThresholdKg = 50;

    private readonly string _aircraftType;
    private readonly List<PositionReport> _pending = [];
    private readonly List<FlightEventReport> _events = [];
    private readonly EpisodeTracker _overspeed = new("Overspeed");
    private readonly EpisodeTracker _stall = new("Stall");
    private readonly EpisodeTracker _slew = new("Slew");
    private readonly EpisodeTracker _pause = new("Pause");
    private readonly EpisodeTracker _simRate = new("SimulationRate");
    private readonly EpisodeTracker _crash = new("Crash");

    private SimSnapshot? _last;
    private DateTime? _lastRecordedAt;
    private (int AglFt, int Flaps)? _configuredSince;
    private DateTime? _taxiInStartedAt;
    private DateTime? _levelSince;
    private int _levelAltitudeFt;
    private double _minFuelKg;
    private double _refueledKg;
    private double? _fuelAtBlockOnKg;

    public FlightRecorder(string aircraftTypeIcao)
    {
        _aircraftType = aircraftTypeIcao;
    }

    public FlightPhase Phase { get; private set; } = FlightPhase.Preflight;
    public SimSnapshot? Last => _last;
    public DateTime? BlockOffAt { get; private set; }
    public DateTime? TakeoffAt { get; private set; }
    public DateTime? LandingAt { get; private set; }
    public DateTime? BlockOnAt { get; private set; }
    public double? FuelAtPushbackKg { get; private set; }
    public int? LandingRateFpm { get; private set; }
    public double? LiftoffPitch { get; private set; }
    public int? RotationSpeedKt { get; private set; }
    public int? TakeoffFlaps { get; private set; }
    public int? LandingFlaps { get; private set; }
    public int? FlapsRetractedAtFt { get; private set; }
    public int? LandingConfiguredAtFt { get; private set; }
    public int? FirstEngineStarted { get; private set; }
    public int? CruiseAltitudeFt { get; private set; }
    public int? TaxiInSeconds { get; private set; }
    public int MaxAltitudeFt { get; private set; }
    public double DistanceNm { get; private set; }
    public bool IncompleteData { get; private set; }
    public IReadOnlyList<FlightEventReport> Events => _events;
    public int PendingPositions => _pending.Count;
    public bool CanComplete => BlockOnAt is not null;
    public bool InFlight => BlockOffAt is not null && BlockOnAt is null;

    public void Update(SimSnapshot s)
    {
        var prev = _last;
        if (prev is not null && s.At <= prev.At) return;

        var flaps = FlapTable.ToDegrees(_aircraftType, s.FlapsHandle, s.FlapDetents);
        var wasInFlight = InFlight;
        var milestone = false;

        if (prev is not null)
        {
            if (wasInFlight && s.At - prev.At > DataGapLimit) IncompleteData = true;
            if (wasInFlight) DistanceNm += Geo.DistanceNm(prev.Latitude, prev.Longitude, s.Latitude, s.Longitude);
            TrackEngineStart(prev, s);
        }

        if (BlockOffAt is null && s.OnGround && !s.ParkingBrake && s.GroundSpeedKt >= 1)
        {
            BlockOffAt = s.At;
            FuelAtPushbackKg = Math.Round(s.FuelKg);
            _minFuelKg = s.FuelKg;
            milestone = true;
        }

        if (BlockOffAt is not null && TakeoffAt is null)
        {
            if (RotationSpeedKt is null && s.OnGround && s.PitchDeg >= RotationPitchDeg && s.IasKt >= LiftoffMinIasKt)
                RotationSpeedKt = s.IasKt;

            if (prev is { OnGround: true } && !s.OnGround && s.IasKt >= LiftoffMinIasKt)
            {
                TakeoffAt = s.At;
                LiftoffPitch = Math.Round(Math.Max(prev.PitchDeg, s.PitchDeg), 1);
                TakeoffFlaps = flaps;
                RotationSpeedKt ??= prev.IasKt;
                milestone = true;
            }
        }

        if (TakeoffAt is not null && LandingAt is null && !s.OnGround)
        {
            if (FlapsRetractedAtFt is null && TakeoffFlaps > 0 && flaps == 0)
                FlapsRetractedAtFt = s.AglFt;

            if (s.GearDown && flaps > 0)
            {
                if (_configuredSince is not { } c || c.Flaps != flaps) _configuredSince = (s.AglFt, flaps.Value);
            }
            else
            {
                _configuredSince = null;
            }

            MaxAltitudeFt = Math.Max(MaxAltitudeFt, s.AltitudeFt);
            TrackCruise(s);
        }

        if (TakeoffAt is not null && LandingAt is null && prev is { OnGround: false } && s.OnGround)
        {
            LandingAt = s.At;
            LandingRateFpm = s.TouchdownRateFpm != 0 ? s.TouchdownRateFpm : prev.VerticalSpeedFpm;
            LandingFlaps = flaps;
            LandingConfiguredAtFt = _configuredSince is { } c && c.Flaps == flaps ? c.AglFt : 0;
            milestone = true;
        }

        if (LandingAt is not null && _taxiInStartedAt is null && s.OnGround && s.GroundSpeedKt < TaxiInBelowKt)
            _taxiInStartedAt = s.At;

        if (InFlight) TrackEvents(s);

        if (LandingAt is not null && BlockOnAt is null && s.OnGround && s.ParkingBrake && s.GroundSpeedKt == 0)
        {
            BlockOnAt = s.At;
            TaxiInSeconds = (int)Math.Round((s.At - (_taxiInStartedAt ?? s.At)).TotalSeconds);
            _fuelAtBlockOnKg = s.FuelKg;
            CloseEpisodes(s.At);
            milestone = true;
        }

        Phase = NextPhase(s);
        Record(s, flaps, milestone);
        _last = s;
    }

    public IReadOnlyList<PositionReport> PeekPositions(int max = LitoralApiClient.MaxPositionsPerBatch) =>
        _pending.Take(max).ToList();

    public void RemovePositions(int count) =>
        _pending.RemoveRange(0, Math.Clamp(count, 0, _pending.Count));

    public double? DistanceFromNm(double latitude, double longitude) =>
        _last is { } s ? Geo.DistanceNm(s.Latitude, s.Longitude, latitude, longitude) : null;

    public CompleteFlightRequest BuildCompletion(string arrivalIcao, bool diversionReported, string? comments, string? flightRoute = null)
    {
        if (BlockOffAt is not DateTime blockOff || BlockOnAt is not DateTime blockOn || _last is not { } last)
            throw new InvalidOperationException("The flight can only be sent after block on.");

        var fuelUsed = FuelAtPushbackKg is double atPushback && _fuelAtBlockOnKg is double atBlockOn
            ? (decimal?)Math.Max(0, Math.Round(atPushback - atBlockOn + _refueledKg))
            : null;

        return new CompleteFlightRequest(
            arrivalIcao.Trim().ToUpperInvariant(),
            blockOff,
            TakeoffAt,
            LandingAt,
            blockOn,
            LandingRateFpm,
            FuelAtPushbackKg is double f ? (decimal)f : null,
            fuelUsed,
            "kg",
            (int)Math.Round(DistanceNm),
            string.IsNullOrWhiteSpace(flightRoute) ? null : flightRoute.Trim(),
            CruiseAltitudeFt,
            null,
            FirstEngineStarted,
            TaxiInSeconds,
            last.AnyEngineRunning,
            LiftoffPitch is double p ? (decimal)p : null,
            RotationSpeedKt,
            TakeoffFlaps,
            LandingFlaps,
            FlapsRetractedAtFt,
            LandingConfiguredAtFt,
            diversionReported,
            IncompleteData,
            string.IsNullOrWhiteSpace(comments) ? null : comments.Trim(),
            _events.ToList());
    }

    private void TrackEngineStart(SimSnapshot prev, SimSnapshot s)
    {
        if (FirstEngineStarted is not null || TakeoffAt is not null) return;

        for (var i = 0; i < s.Engines.Count; i++)
        {
            var before = i < prev.Engines.Count && prev.Engines[i];
            if (!before && s.Engines[i])
            {
                FirstEngineStarted = i + 1;
                return;
            }
        }
    }

    private void TrackCruise(SimSnapshot s)
    {
        if (Math.Abs(s.VerticalSpeedFpm) >= ClimbDescentFpm)
        {
            _levelSince = null;
            return;
        }

        if (_levelSince is null || Math.Abs(s.AltitudeFt - _levelAltitudeFt) > LevelBandFt)
        {
            _levelSince = s.At;
            _levelAltitudeFt = s.AltitudeFt;
            return;
        }

        if (s.At - _levelSince.Value < CruiseHold) return;

        var level = (int)Math.Round(_levelAltitudeFt / 100d) * 100;
        if (CruiseAltitudeFt is null || level > CruiseAltitudeFt) CruiseAltitudeFt = level;
    }

    private void TrackEvents(SimSnapshot s)
    {
        Add(_overspeed.Update(s.Overspeed, s.At));
        Add(_stall.Update(s.Stall, s.At));
        Add(_slew.Update(s.Slew, s.At));
        Add(_pause.Update(s.Paused, s.At));
        Add(_simRate.Update(Math.Abs(s.SimRate - 1) > 0.01, s.At, (decimal)s.SimRate));
        Add(_crash.Update(s.Crashed, s.At));

        _minFuelKg = Math.Min(_minFuelKg, s.FuelKg);
        var added = s.FuelKg - _minFuelKg;
        if (added >= RefuelThresholdKg)
        {
            _events.Add(new FlightEventReport("Refuel", s.At, 0, (decimal)Math.Round(added), null));
            _refueledKg += added;
            _minFuelKg = s.FuelKg;
        }
    }

    private void CloseEpisodes(DateTime at)
    {
        foreach (var tracker in new[] { _overspeed, _stall, _slew, _pause, _simRate, _crash })
            Add(tracker.Close(at));
    }

    private void Add(FlightEventReport? report)
    {
        if (report is not null) _events.Add(report);
    }

    private FlightPhase NextPhase(SimSnapshot s)
    {
        if (BlockOnAt is not null) return FlightPhase.Arrived;
        if (BlockOffAt is null) return FlightPhase.Preflight;
        if (LandingAt is not null) return _taxiInStartedAt is not null ? FlightPhase.TaxiIn : FlightPhase.Landing;
        if (TakeoffAt is null) return s.OnGround && s.IasKt >= TakeoffRollKt ? FlightPhase.Takeoff : FlightPhase.TaxiOut;

        if (s.VerticalSpeedFpm >= ClimbDescentFpm) return FlightPhase.Climb;
        if (s.VerticalSpeedFpm <= -ClimbDescentFpm) return s.AglFt < ApproachAglFt ? FlightPhase.Approach : FlightPhase.Descent;
        if (Phase == FlightPhase.Approach) return FlightPhase.Approach;
        if (s.AglFt >= ApproachAglFt && s.AltitudeFt >= MaxAltitudeFt - 1000) return FlightPhase.Cruise;
        return Phase == FlightPhase.Takeoff ? FlightPhase.Climb : Phase;
    }

    private void Record(SimSnapshot s, int? flaps, bool force)
    {
        var interval = !InFlight && !force
            ? GateInterval
            : Phase is FlightPhase.Takeoff or FlightPhase.Landing || (!s.OnGround && s.AglFt < LowAltitudeAglFt)
                ? LowAltitudeInterval
                : GroundInterval;

        if (!force && _lastRecordedAt is DateTime last && s.At - last < interval) return;

        _pending.Add(new PositionReport(
            s.At,
            Math.Round(s.Latitude, 6),
            Math.Round(s.Longitude, 6),
            s.AltitudeFt,
            s.AglFt,
            s.IasKt,
            s.GroundSpeedKt,
            s.HeadingTrue,
            s.VerticalSpeedFpm,
            flaps,
            s.GearDown,
            s.OnGround,
            Phase.ToString()));
        _lastRecordedAt = s.At;
    }
}