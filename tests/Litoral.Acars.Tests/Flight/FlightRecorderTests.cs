using Litoral.Acars.Core.Flight;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests.Flight;

public class FlightRecorderTests
{
    private static FlightRecorder Fly(IEnumerable<SimSnapshot> snapshots, string type = TestFlights.Type)
    {
        var recorder = new FlightRecorder(type);
        foreach (var s in snapshots) recorder.Update(s);
        return recorder;
    }

    private static List<SimSnapshot> Change(List<SimSnapshot> script, Func<SimSnapshot, bool> where, Func<SimSnapshot, SimSnapshot> change, int take = int.MaxValue)
    {
        var done = 0;
        var result = new List<SimSnapshot>(script.Count);
        foreach (var s in script)
        {
            if (done < take && where(s))
            {
                result.Add(change(s));
                done++;
            }
            else
            {
                result.Add(s);
            }
        }
        return result;
    }

    private static bool Cruising(SimSnapshot s) => !s.OnGround && s.VerticalSpeedFpm == 0;

    [Fact]
    public void A_clean_flight_records_the_four_block_times_in_order()
    {
        var r = Fly(TestFlights.Script());

        Assert.NotNull(r.BlockOffAt);
        Assert.True(r.BlockOffAt < r.TakeoffAt);
        Assert.True(r.TakeoffAt < r.LandingAt);
        Assert.True(r.LandingAt < r.BlockOnAt);
        Assert.Equal(FlightPhase.Arrived, r.Phase);
        Assert.True(r.CanComplete);
    }

    [Fact]
    public void Block_off_is_the_first_movement_with_the_parking_brake_released()
    {
        var script = TestFlights.Script();
        var r = Fly(script);

        var first = script.First(s => !s.ParkingBrake && s.GroundSpeedKt > 0);
        Assert.Equal(first.At, r.BlockOffAt);
        Assert.Equal(Math.Round(first.FuelKg), r.FuelAtPushbackKg);
    }

    [Fact]
    public void Takeoff_data_is_captured_at_liftoff()
    {
        var r = Fly(TestFlights.Script());

        Assert.Equal(5, r.TakeoffFlaps);
        Assert.Equal(140, r.RotationSpeedKt);
        Assert.Equal(8.0, r.LiftoffPitch);
        Assert.InRange(r.FlapsRetractedAtFt!.Value, 1950, 2050);
    }

    [Fact]
    public void Landing_data_is_captured_at_touchdown()
    {
        var r = Fly(TestFlights.Script());

        Assert.Equal(-150, r.LandingRateFpm);
        Assert.Equal(30, r.LandingFlaps);
        Assert.InRange(r.LandingConfiguredAtFt!.Value, 1750, 1800);
    }

    [Fact]
    public void The_first_engine_started_is_recorded()
    {
        Assert.Equal(2, Fly(TestFlights.Script()).FirstEngineStarted);
    }

    [Fact]
    public void Cruise_altitude_is_the_highest_level_held_for_two_minutes()
    {
        Assert.Equal(10000, Fly(TestFlights.Script()).CruiseAltitudeFt);
    }

    [Fact]
    public void A_level_off_shorter_than_two_minutes_is_not_cruise()
    {
        var script = TestFlights.Script();
        var cruise = script.Where(Cruising).ToList();
        var trimmed = script.Where(s => !Cruising(s) || s.At <= cruise[0].At.AddSeconds(90)).ToList();

        Assert.Null(Fly(trimmed).CruiseAltitudeFt);
    }

    [Fact]
    public void Taxi_in_runs_from_leaving_the_runway_speed_until_block_on()
    {
        var script = TestFlights.Script();
        var r = Fly(script);

        var exit = script.First(s => r.LandingAt < s.At && s.OnGround && s.GroundSpeedKt < FlightRecorder.TaxiInBelowKt);
        Assert.Equal((int)(r.BlockOnAt!.Value - exit.At).TotalSeconds, r.TaxiInSeconds);
    }

    [Fact]
    public void Fuel_and_distance_are_summed_over_the_flight()
    {
        var r = Fly(TestFlights.Script());
        var request = r.BuildCompletion("SBFL", false, null);

        Assert.Equal("kg", request.FuelUnit);
        Assert.True(request.FuelUsed > 0);
        Assert.InRange(request.DistanceNm!.Value, 275, 281);
    }

    [Fact]
    public void A_clean_flight_has_no_events_and_complete_data()
    {
        var r = Fly(TestFlights.Script());

        Assert.Empty(r.Events);
        Assert.False(r.IncompleteData);
    }

    [Fact]
    public void Every_phase_is_reported_in_order()
    {
        var r = Fly(TestFlights.Script());
        var phases = r.PeekPositions(int.MaxValue).Select(p => p.Phase!).Distinct().ToList();

        Assert.Equal(new[] { "Preflight", "TaxiOut", "Takeoff", "Climb", "Cruise", "Descent", "Approach", "Landing", "TaxiIn", "Arrived" }, phases);
    }

    [Fact]
    public void Positions_carry_flap_degrees_gear_and_ground_state()
    {
        var r = Fly(TestFlights.Script());
        var positions = r.PeekPositions(int.MaxValue);

        Assert.Contains(5, positions.Select(p => p.Flaps ?? -1));
        Assert.Contains(30, positions.Select(p => p.Flaps ?? -1));
        Assert.Contains(positions, p => p.GearDown == false && p.OnGround == false);
        Assert.All(positions, p => Assert.InRange(p.Heading, 0, 359));
    }

    [Fact]
    public void Positions_are_sampled_every_second_low_and_every_five_seconds_high()
    {
        var r = Fly(TestFlights.Script());
        var positions = r.PeekPositions(int.MaxValue);

        var low = positions.Where(p => p.OnGround == false && p.AglFt < FlightRecorder.LowAltitudeAglFt).ToList();
        var high = positions.Where(p => p.Phase == "Cruise").ToList();

        Assert.True(low.Zip(low.Skip(1)).Where(x => x.Second.At - x.First.At < TimeSpan.FromSeconds(10)).All(x => x.Second.At - x.First.At == TimeSpan.FromSeconds(1)));
        Assert.All(high.Zip(high.Skip(1)), x => Assert.Equal(TimeSpan.FromSeconds(5), x.Second.At - x.First.At));
    }

    [Fact]
    public void At_the_gate_positions_are_sampled_every_thirty_seconds()
    {
        var r = Fly(TestFlights.Script().Take(60));

        Assert.Equal(2, r.PendingPositions);
        Assert.Equal(FlightPhase.Preflight, r.Phase);
    }

    [Fact]
    public void Sent_positions_are_removed_from_the_queue()
    {
        var r = Fly(TestFlights.Script());
        var total = r.PendingPositions;

        var batch = r.PeekPositions(100);
        r.RemovePositions(batch.Count);

        Assert.Equal(100, batch.Count);
        Assert.Equal(total - 100, r.PendingPositions);
        Assert.NotEqual(batch[0].At, r.PeekPositions(1)[0].At);
    }

    [Fact]
    public void A_bounce_does_not_count_as_a_second_landing()
    {
        var script = TestFlights.Script();
        var touchdown = script.FindIndex(s => s.OnGround && s.TouchdownRateFpm != 0);
        script[touchdown + 2] = script[touchdown + 2] with { OnGround = false, AglFt = 5, TouchdownRateFpm = -400 };
        script[touchdown + 3] = script[touchdown + 3] with { TouchdownRateFpm = -400 };

        var r = Fly(script);

        Assert.Equal(script[touchdown].At, r.LandingAt);
        Assert.Equal(-150, r.LandingRateFpm);
    }

    [Fact]
    public void A_gear_up_landing_counts_as_configured_at_zero_feet()
    {
        var script = TestFlights.Script();
        var touchdown = script.FindIndex(s => s.OnGround && s.TouchdownRateFpm != 0);
        script = script.Select((s, i) => i < touchdown && !s.OnGround && s.VerticalSpeedFpm < 0 ? s with { GearDown = false } : s).ToList();

        Assert.Equal(0, Fly(script).LandingConfiguredAtFt);
    }

    [Fact]
    public void A_late_flap_change_moves_the_configured_height()
    {
        var script = TestFlights.Script();
        script = script.Select(s => !s.OnGround && s.VerticalSpeedFpm < 0 && s.AglFt is < 1800 and >= 600 ? s with { FlapsHandle = FlapTable.ToHandle(TestFlights.Type, 15) } : s).ToList();

        Assert.InRange(Fly(script).LandingConfiguredAtFt!.Value, 580, 600);
    }

    [Fact]
    public void Ground_bumps_while_taxiing_are_not_a_takeoff()
    {
        var script = TestFlights.Script();
        var taxi = script.FindIndex(s => s.GroundSpeedKt == 15);
        script[taxi + 10] = script[taxi + 10] with { OnGround = false };

        var r = Fly(script);

        Assert.True(r.TakeoffAt > script[taxi + 10].At);
    }

    [Fact]
    public void A_pause_in_flight_becomes_an_event_with_its_duration()
    {
        var script = Change(TestFlights.Script(), Cruising, s => s with { Paused = true }, take: 40);

        var pause = Assert.Single(Fly(script).Events);

        Assert.Equal("Pause", pause.Type);
        Assert.Equal(40, pause.DurationSeconds);
    }

    [Fact]
    public void A_pause_at_the_gate_is_not_reported()
    {
        var script = Change(TestFlights.Script(), s => s.ParkingBrake && s.GroundSpeedKt == 0 && s.At < TestFlights.Start.AddSeconds(50), s => s with { Paused = true });

        Assert.Empty(Fly(script).Events);
    }

    [Fact]
    public void Simulation_rate_keeps_the_highest_rate_used()
    {
        var script = TestFlights.Script();
        var i = script.FindIndex(Cruising);
        for (var k = 0; k < 30; k++) script[i + k] = script[i + k] with { SimRate = k < 10 ? 2 : 4 };

        var ev = Assert.Single(Fly(script).Events);

        Assert.Equal("SimulationRate", ev.Type);
        Assert.Equal(4m, ev.Value);
        Assert.Equal(30, ev.DurationSeconds);
    }

    [Theory]
    [InlineData("Overspeed")]
    [InlineData("Stall")]
    [InlineData("Slew")]
    [InlineData("Crash")]
    public void Warnings_become_events(string type)
    {
        var script = Change(TestFlights.Script(), Cruising, s => type switch
        {
            "Overspeed" => s with { Overspeed = true },
            "Stall" => s with { Stall = true },
            "Slew" => s with { Slew = true },
            _ => s with { Crashed = true }
        }, take: 12);

        var ev = Assert.Single(Fly(script).Events);

        Assert.Equal(type, ev.Type);
        Assert.Equal(12, ev.DurationSeconds);
    }

    [Fact]
    public void An_episode_still_open_at_block_on_is_closed_there()
    {
        var script = TestFlights.Script();
        var touchdown = script.FindIndex(s => s.OnGround && s.TouchdownRateFpm != 0);
        script = script.Select((s, i) => i >= touchdown ? s with { Overspeed = true } : s).ToList();

        var r = Fly(script);
        var ev = Assert.Single(r.Events);

        Assert.Equal(r.BlockOnAt, ev.At.AddSeconds(ev.DurationSeconds));
    }

    [Fact]
    public void Fuel_added_in_flight_is_a_refuel_and_counts_as_used()
    {
        var clean = Fly(TestFlights.Script()).BuildCompletion("SBFL", false, null);

        var script = TestFlights.Script();
        var i = script.FindIndex(Cruising);
        script = script.Select((s, k) => k >= i ? s with { FuelKg = s.FuelKg + 1000 } : s).ToList();
        var r = Fly(script);

        var refuel = Assert.Single(r.Events);
        Assert.Equal("Refuel", refuel.Type);
        Assert.InRange(refuel.Value!.Value, 999m, 1001m);
        Assert.InRange(r.BuildCompletion("SBFL", false, null).FuelUsed!.Value, clean.FuelUsed!.Value - 2, clean.FuelUsed.Value + 2);
    }

    [Fact]
    public void Small_fuel_jitter_is_not_a_refuel()
    {
        var script = TestFlights.Script();
        var i = script.FindIndex(Cruising);
        script[i] = script[i] with { FuelKg = script[i].FuelKg + 20 };

        Assert.Empty(Fly(script).Events);
    }

    [Fact]
    public void A_data_gap_over_a_minute_in_flight_marks_the_data_incomplete()
    {
        var script = TestFlights.Script();
        var i = script.FindIndex(Cruising);
        var gapped = script.Where((s, k) => k < i || k > i + 61).ToList();

        Assert.True(Fly(gapped).IncompleteData);
    }

    [Fact]
    public void A_gap_before_block_off_is_fine()
    {
        var script = TestFlights.Script();
        var gapped = script.Where((s, k) => k is < 5 or > 50).ToList();

        Assert.False(Fly(gapped).IncompleteData);
    }

    [Fact]
    public void Old_or_repeated_snapshots_are_ignored()
    {
        var script = TestFlights.Script();
        var r = new FlightRecorder(TestFlights.Type);
        foreach (var s in script.Take(200)) r.Update(s);
        var count = r.PendingPositions;

        r.Update(script[10] with { OnGround = false });
        r.Update(script[199]);

        Assert.Equal(count, r.PendingPositions);
        Assert.Equal(script[199], r.Last);
    }

    [Fact]
    public void An_aircraft_type_without_a_flap_table_reports_no_flaps()
    {
        var r = Fly(TestFlights.Script(), type: "A320");

        Assert.Null(r.TakeoffFlaps);
        Assert.Null(r.LandingFlaps);
        Assert.All(r.PeekPositions(int.MaxValue), p => Assert.Null(p.Flaps));
    }

    [Fact]
    public void The_777_uses_its_own_flap_detents()
    {
        var options = TestFlights.Options("B77W") with { TakeoffFlaps = 15, LandingFlaps = 25 };
        var r = Fly(TestFlights.Script(options), type: "B77W");

        Assert.Equal(15, r.TakeoffFlaps);
        Assert.Equal(25, r.LandingFlaps);
    }

    [Fact]
    public void The_completion_carries_everything_recorded()
    {
        var r = Fly(TestFlights.Script());

        var c = r.BuildCompletion(" sbfl ", false, "  Smooth flight ");

        Assert.Equal("SBFL", c.ArrivalIcao);
        Assert.Equal(r.BlockOffAt, c.BlockOffAt);
        Assert.Equal(r.BlockOnAt, c.BlockOnAt);
        Assert.Equal(-150, c.LandingRateFpm);
        Assert.Equal(5, c.TakeoffFlaps);
        Assert.Equal(30, c.LandingFlaps);
        Assert.Equal(8.0m, c.LiftoffPitch);
        Assert.Equal(2, c.FirstEngineStarted);
        Assert.Equal(10000, c.CruiseAltitudeFt);
        Assert.False(c.EnginesRunningAtLog);
        Assert.False(c.DiversionReported);
        Assert.Null(c.OnlinePercent);
        Assert.Equal("Smooth flight", c.Comments);
        Assert.NotNull(c.Events);
    }

    [Fact]
    public void Engines_still_running_at_log_are_reported()
    {
        var script = TestFlights.Script();
        script = script.Select(s => s.At > script[^30].At ? s with { Engines = [true, true] } : s).ToList();

        Assert.True(Fly(script).BuildCompletion("SBFL", false, null).EnginesRunningAtLog);
    }

    [Fact]
    public void The_flight_cannot_be_completed_before_block_on()
    {
        var script = TestFlights.Script();
        var r = Fly(script.Take(script.Count - 30));

        Assert.False(r.CanComplete);
        Assert.Throws<InvalidOperationException>(() => r.BuildCompletion("SBFL", false, null));
    }

    [Fact]
    public void Distance_from_a_point_uses_the_last_position()
    {
        var r = Fly(TestFlights.Script());

        Assert.InRange(r.DistanceFromNm(TestFlights.SbflLat, TestFlights.SbflLon)!.Value, 0, 0.01);
        Assert.Null(new FlightRecorder(TestFlights.Type).DistanceFromNm(0, 0));
    }
}