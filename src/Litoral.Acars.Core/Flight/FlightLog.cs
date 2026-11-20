using System.Globalization;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Core.Flight;

public enum FlightLogLevel
{
    Info,
    Milestone,
    Warning
}

public sealed record FlightLogEntry(DateTime At, FlightLogLevel Level, string Message)
{
    public string Time => At.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z";
    public override string ToString() => $"{At:yyyy-MM-dd HH:mm:ss}Z  {Level,-9}  {Message}";
}

public sealed class FlightLog
{
    public const int MaxEntries = 1000;
    public const int TransitionAltitudeFt = 10000;
    public const int SpeedLimitKt = 250;
    public const int SpeedLimitToleranceKt = 10;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly List<FlightLogEntry> _entries = [];
    private readonly string _aircraftType;
    private SimSnapshot? _last;
    private int? _lastFlaps;
    private FlightPhase _phase = FlightPhase.Preflight;
    private int? _cruiseFt;
    private int _refuels;
    private bool _incomplete;
    private bool _blockOff, _takeoff, _landing, _blockOn;
    private DateTime? _overspeedSince, _stallSince, _slewSince, _pauseSince, _crashSince, _rateSince, _speedLimitSince;

    public FlightLog(string aircraftTypeIcao)
    {
        _aircraftType = aircraftTypeIcao;
    }

    public event Action<FlightLogEntry>? Added;

    public IReadOnlyList<FlightLogEntry> Entries => _entries;

    public void Info(DateTime at, string message) => Write(at, FlightLogLevel.Info, message);

    public void Warning(DateTime at, string message) => Write(at, FlightLogLevel.Warning, message);

    public void Observe(SimSnapshot s, FlightRecorder r)
    {
        var flaps = FlapTable.ToDegrees(_aircraftType, s.FlapsHandle, s.FlapDetents);
        var prev = _last;

        if (prev is null)
        {
            Info(s.At, $"Recording started at {(s.OnGround ? "the gate" : Altitude(s.AltitudeFt))} · fuel {Kg(s.FuelKg)}"
                + (s.ParkingBrake ? " · parking brake set" : ""));
        }
        else
        {
            Engines(prev, s);
            Systems(prev, s, flaps);
        }

        Milestones(s, r, flaps);
        Abnormals(s);

        if (r.Phase != _phase)
        {
            _phase = r.Phase;
            Info(s.At, $"Phase: {PhaseName(r.Phase)}");
        }

        if (r.CruiseAltitudeFt is int cruise && cruise != _cruiseFt)
        {
            _cruiseFt = cruise;
            Write(s.At, FlightLogLevel.Milestone, $"Level at {Altitude(cruise)}");
        }

        var refuels = r.Events.Where(e => e.Type == "Refuel").ToList();
        for (var i = _refuels; i < refuels.Count; i++)
            Warning(s.At, $"Fuel added in flight: {Kg((double)(refuels[i].Value ?? 0))}");
        _refuels = refuels.Count;

        if (r.IncompleteData && !_incomplete)
        {
            _incomplete = true;
            Warning(s.At, $"No data for more than {FlightRecorder.DataGapLimit.TotalSeconds:0} s. The flight is marked as incomplete.");
        }

        _last = s;
        _lastFlaps = flaps;
    }

    private void Engines(SimSnapshot prev, SimSnapshot s)
    {
        for (var i = 0; i < s.Engines.Count; i++)
        {
            var before = i < prev.Engines.Count && prev.Engines[i];
            if (before == s.Engines[i]) continue;
            Info(s.At, s.Engines[i] ? $"Engine {i + 1} started" : $"Engine {i + 1} shut down");
        }
    }

    private void Systems(SimSnapshot prev, SimSnapshot s, int? flaps)
    {
        if (prev.ParkingBrake != s.ParkingBrake && s.OnGround)
            Info(s.At, s.ParkingBrake ? "Parking brake set" : "Parking brake released");

        if (prev.GearDown != s.GearDown)
            Info(s.At, $"Gear {(s.GearDown ? "down" : "up")} · {s.IasKt} kt · {Altitude(s.AltitudeFt)}");

        if (flaps != _lastFlaps && flaps is not null && _lastFlaps is not null)
            Info(s.At, $"Flaps {_lastFlaps} → {flaps} · {s.IasKt} kt");

        var climbing = prev.AltitudeFt < TransitionAltitudeFt && s.AltitudeFt >= TransitionAltitudeFt;
        var descending = prev.AltitudeFt >= TransitionAltitudeFt && s.AltitudeFt < TransitionAltitudeFt;
        if (!s.OnGround && (climbing || descending))
            Info(s.At, $"Passing 10,000 ft {(climbing ? "climbing" : "descending")} · {s.IasKt} kt");
    }

    private void Milestones(SimSnapshot s, FlightRecorder r, int? flaps)
    {
        if (!_blockOff && r.BlockOffAt is not null)
        {
            _blockOff = true;
            Write(s.At, FlightLogLevel.Milestone, $"Block off · fuel {Kg(r.FuelAtPushbackKg ?? s.FuelKg)}");
        }

        if (!_takeoff && r.TakeoffAt is not null)
        {
            _takeoff = true;
            var vr = r.RotationSpeedKt is int kt ? $"VR {kt} kt · " : "";
            var pitch = r.LiftoffPitch is double p ? $"pitch {p.ToString("0.0", Inv)}° · " : "";
            Write(s.At, FlightLogLevel.Milestone, $"Takeoff · {vr}{pitch}flaps {r.TakeoffFlaps?.ToString(Inv) ?? "?"}");
        }

        if (!_landing && r.LandingAt is not null)
        {
            _landing = true;
            var rate = r.LandingRateFpm is int fpm ? $"{fpm} fpm" : "rate unknown";
            Write(s.At, FlightLogLevel.Milestone, $"Touchdown · {rate} · {s.IasKt} kt · flaps {r.LandingFlaps?.ToString(Inv) ?? flaps?.ToString(Inv) ?? "?"}");
        }

        if (!_blockOn && r.BlockOnAt is not null)
        {
            _blockOn = true;
            var used = r.FuelAtPushbackKg is double atPushback ? $" · fuel used {Kg(Math.Max(0, atPushback - s.FuelKg))}" : "";
            Write(s.At, FlightLogLevel.Milestone, $"Block on · {r.DistanceNm.ToString("0", Inv)} NM flown{used}");
        }
    }

    private void Abnormals(SimSnapshot s)
    {
        Episode(ref _overspeedSince, s.Overspeed, s.At, "Overspeed", "Overspeed", FlightLogLevel.Warning);
        Episode(ref _stallSince, s.Stall, s.At, "Stall warning", "Stall warning", FlightLogLevel.Warning);
        Episode(ref _slewSince, s.Slew, s.At, "Slew mode", "Slew mode", FlightLogLevel.Warning);
        Episode(ref _crashSince, s.Crashed, s.At, "Crash", "Crash", FlightLogLevel.Warning);
        Episode(ref _pauseSince, s.Paused, s.At, "Simulator paused", "Pause", FlightLogLevel.Info);
        Episode(ref _rateSince, Math.Abs(s.SimRate - 1) > 0.01, s.At,
            $"Simulation rate {s.SimRate.ToString("0.##", Inv)}x", "Simulation rate change", FlightLogLevel.Warning);

        var tooFast = !s.OnGround && s.AltitudeFt < TransitionAltitudeFt && s.IasKt > SpeedLimitKt + SpeedLimitToleranceKt;
        Episode(ref _speedLimitSince, tooFast, s.At,
            $"Above 250 kt below 10,000 ft ({s.IasKt} kt)", "Above 250 kt below 10,000 ft", FlightLogLevel.Warning);
    }

    private void Episode(ref DateTime? since, bool active, DateTime at, string started, string name, FlightLogLevel level)
    {
        if (active && since is null)
        {
            since = at;
            Write(at, level, started);
        }
        else if (!active && since is DateTime start)
        {
            since = null;
            Info(at, $"{name} ended · {(int)Math.Round((at - start).TotalSeconds)} s");
        }
    }

    private void Write(DateTime at, FlightLogLevel level, string message)
    {
        var entry = new FlightLogEntry(at, level, message);
        _entries.Add(entry);
        if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        Added?.Invoke(entry);
    }

    public static string PhaseName(FlightPhase phase) => phase switch
    {
        FlightPhase.TaxiOut => "Taxi out",
        FlightPhase.TaxiIn => "Taxi in",
        _ => phase.ToString()
    };

    private static string Kg(double kg) => $"{Math.Round(kg).ToString("N0", Inv)} kg";

    private static string Altitude(int ft) => ft >= TransitionAltitudeFt
        ? $"FL{(int)Math.Round(ft / 100d):000}"
        : $"{ft.ToString("N0", Inv)} ft";
}