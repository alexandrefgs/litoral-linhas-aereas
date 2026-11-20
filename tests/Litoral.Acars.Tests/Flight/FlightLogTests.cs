using Litoral.Acars.Core.Flight;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests.Flight;

public class FlightLogTests
{
    private static (FlightRecorder Recorder, FlightLog Log) Run(IEnumerable<SimSnapshot> snapshots)
    {
        var recorder = new FlightRecorder(TestFlights.Type);
        var log = new FlightLog(TestFlights.Type);
        foreach (var s in snapshots)
        {
            recorder.Update(s);
            log.Observe(s, recorder);
        }
        return (recorder, log);
    }

    private static SimSnapshot Air(int seconds, int altitudeFt = 20000, int iasKt = 280) => new(
        TestFlights.Start.AddSeconds(seconds), TestFlights.SbgrLat, TestFlights.SbgrLon, altitudeFt, altitudeFt, iasKt, iasKt + 50, 180, 0, 2,
        0, 0, false, false, false, [true, true], 5000, 0, false, false, false, false, 1, false);

    private static List<string> Messages(FlightLog log) => log.Entries.Select(e => e.Message).ToList();

    [Fact]
    public void A_scripted_flight_logs_the_milestones_in_order()
    {
        var (_, log) = Run(TestFlights.Script());

        var milestones = log.Entries.Where(e => e.Level == FlightLogLevel.Milestone).Select(e => e.Message.Split(' ')[0]).ToList();

        string[] expected = ["Block", "Takeoff", "Level", "Touchdown", "Block"];
        Assert.Equal(expected, milestones);
        Assert.StartsWith("Block off", log.Entries.First(e => e.Level == FlightLogLevel.Milestone).Message);
        Assert.StartsWith("Block on", log.Entries.Last(e => e.Level == FlightLogLevel.Milestone).Message);
    }

    [Fact]
    public void Takeoff_and_touchdown_carry_the_numbers()
    {
        var (recorder, log) = Run(TestFlights.Script());
        var messages = Messages(log);

        Assert.Contains(messages, m => m.StartsWith("Takeoff · VR ") && m.EndsWith($"flaps {recorder.TakeoffFlaps}"));
        Assert.Contains($"Touchdown · {recorder.LandingRateFpm} fpm", messages.Single(m => m.StartsWith("Touchdown")));
        Assert.Contains("NM flown · fuel used", messages.Single(m => m.StartsWith("Block on")));
    }

    [Fact]
    public void Engines_brake_gear_and_flaps_are_logged()
    {
        var messages = Messages(Run(TestFlights.Script()).Log);

        Assert.Contains("Engine 1 started", messages);
        Assert.Contains("Engine 1 shut down", messages);
        Assert.Contains("Parking brake released", messages);
        Assert.Contains("Parking brake set", messages);
        Assert.Contains(messages, m => m.StartsWith("Gear up"));
        Assert.Contains(messages, m => m.StartsWith("Gear down"));
        Assert.Contains(messages, m => m.StartsWith("Flaps 0 → 5"));
        Assert.Contains(messages, m => m.StartsWith("Passing 10,000 ft climbing"));
        Assert.Contains(messages, m => m.StartsWith("Passing 10,000 ft descending"));
    }

    [Fact]
    public void Each_phase_change_is_logged_once()
    {
        var phases = Messages(Run(TestFlights.Script()).Log).Where(m => m.StartsWith("Phase: ")).ToList();

        Assert.Equal(phases.Distinct().Count(), phases.Count);
        Assert.Equal("Phase: Taxi out", phases[0]);
        Assert.Equal("Phase: Arrived", phases[^1]);
    }

    [Fact]
    public void A_clean_flight_has_no_warnings() =>
        Assert.DoesNotContain(Run(TestFlights.Script()).Log.Entries, e => e.Level == FlightLogLevel.Warning);

    [Fact]
    public void An_overspeed_is_logged_when_it_starts_and_ends_with_its_duration()
    {
        var (_, log) = Run([Air(0), Air(1) with { Overspeed = true }, Air(5) with { Overspeed = true }, Air(13)]);

        Assert.Contains(log.Entries, e => e is { Level: FlightLogLevel.Warning, Message: "Overspeed" });
        Assert.Contains("Overspeed ended · 12 s", Messages(log));
    }

    [Fact]
    public void Pause_is_info_and_sim_rate_is_a_warning()
    {
        var (_, log) = Run([Air(0), Air(1) with { Paused = true }, Air(4), Air(5) with { SimRate = 4 }, Air(65)]);

        Assert.Contains(log.Entries, e => e is { Level: FlightLogLevel.Info, Message: "Simulator paused" });
        Assert.Contains(log.Entries, e => e is { Level: FlightLogLevel.Warning, Message: "Simulation rate 4x" });
        Assert.Contains("Simulation rate change ended · 60 s", Messages(log));
    }

    [Fact]
    public void Flying_faster_than_250_kt_below_10000_ft_is_a_warning()
    {
        var (_, log) = Run([Air(0, 8000, 250), Air(1, 8000, 290), Air(2, 8000, 300), Air(3, 8000, 240)]);

        Assert.Single(log.Entries, e => e.Level == FlightLogLevel.Warning);
        Assert.Contains("Above 250 kt below 10,000 ft (290 kt)", Messages(log));
        Assert.Contains("Above 250 kt below 10,000 ft ended · 2 s", Messages(log));
    }

    [Fact]
    public void Small_speed_excess_is_tolerated() =>
        Assert.DoesNotContain(Run([Air(0, 8000, 255), Air(1, 8000, 260)]).Log.Entries, e => e.Level == FlightLogLevel.Warning);

    [Fact]
    public void Each_entry_is_announced_and_has_a_zulu_time()
    {
        var log = new FlightLog(TestFlights.Type);
        var heard = new List<FlightLogEntry>();
        log.Added += heard.Add;

        log.Info(new DateTime(2026, 11, 19, 21, 5, 9, DateTimeKind.Utc), "Flight loaded");

        var entry = Assert.Single(heard);
        Assert.Equal("21:05:09Z", entry.Time);
        Assert.Equal("2026-11-19 21:05:09Z  Info       Flight loaded", entry.ToString());
    }

    [Fact]
    public void The_log_keeps_only_the_latest_entries()
    {
        var log = new FlightLog(TestFlights.Type);
        for (var i = 0; i < FlightLog.MaxEntries + 5; i++) log.Info(TestFlights.Start, $"#{i}");

        Assert.Equal(FlightLog.MaxEntries, log.Entries.Count);
        Assert.Equal("#5", log.Entries[0].Message);
    }

    [Theory]
    [InlineData(FlightPhase.TaxiOut, "Taxi out")]
    [InlineData(FlightPhase.TaxiIn, "Taxi in")]
    [InlineData(FlightPhase.Cruise, "Cruise")]
    public void Phase_names_are_readable(FlightPhase phase, string expected) =>
        Assert.Equal(expected, FlightLog.PhaseName(phase));
}