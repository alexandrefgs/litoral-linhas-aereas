using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using static Litoral.Domain.Tests.Scoring.ScoringTestData;

namespace Litoral.Domain.Tests.Scoring;

public class PirepEvaluatorTests
{
    [Fact]
    public void A_clean_flight_without_rules_scores_nothing()
    {
        var result = Evaluate(Flight());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Points);
        Assert.False(result.Rejected);
        Assert.Equal(100, result.Conformity);
    }

    [Fact]
    public void Inactive_rules_are_ignored()
    {
        var rule = Points(ScoringCategory.FlightTime, ScoringCheck.BlockTime, 30, max: 600);
        rule.Deactivate();

        var result = Evaluate(Flight() with { BlockMinutes = 90 }, rule);

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Rules_for_other_aircraft_types_are_ignored()
    {
        var rule = Reject(ScoringCategory.Takeoff, ScoringCheck.TailStrike, min: 10, types: "B77W");

        var result = Evaluate(Flight("B738") with { LiftoffPitch = 12 }, rule);

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Review_flight_and_pilot_rules_are_not_applied_by_the_evaluator()
    {
        var result = Evaluate(Flight(),
            Reject(ScoringCategory.Rejection, ScoringCheck.MissingRemarks, text: "OPR/LITORAL"),
            Points(ScoringCategory.CrewDuty, ScoringCheck.CrewDuty, 10),
            Points(ScoringCategory.Cancellation, ScoringCheck.BookingCancelled, -500));

        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData(45, 15)]
    [InlineData(59, 15)]
    [InlineData(60, 30)]
    [InlineData(119, 30)]
    [InlineData(400, 100)]
    public void Block_time_picks_the_matching_band(int minutes, int expected)
    {
        var result = Evaluate(Flight() with { BlockMinutes = minutes },
            Points(ScoringCategory.FlightTime, ScoringCheck.BlockTime, 15, max: 59),
            Points(ScoringCategory.FlightTime, ScoringCheck.BlockTime, 30, min: 60, max: 119),
            Points(ScoringCategory.FlightTime, ScoringCheck.BlockTime, 100, min: 360));

        Assert.Equal(expected, result.Points);
    }

    [Theory]
    [InlineData(-120, 60)]
    [InlineData(-450, -50)]
    [InlineData(-30, 40)]
    public void Touchdown_rate_uses_the_positive_sink_rate(int landingRate, int expected)
    {
        var result = Evaluate(Flight() with { LandingRateFpm = landingRate },
            Points(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, 40, min: 1, max: 50),
            Points(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, 60, min: 101, max: 150),
            Points(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, -50, min: 401, max: 500));

        Assert.Equal(expected, result.Points);
    }

    [Fact]
    public void A_hard_landing_rejects_the_flight()
    {
        var result = Evaluate(Flight() with { LandingRateFpm = -620 },
            Reject(ScoringCategory.Rejection, ScoringCheck.TouchdownRate, min: 501));

        Assert.True(result.Rejected);
        Assert.Equal(0, result.Conformity);
    }

    [Fact]
    public void Missing_landing_rate_scores_no_touchdown_band()
    {
        var result = Evaluate(Flight(), Points(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, 0, max: 0));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Flaps_overspeed_counts_each_episode_on_that_flap_position()
    {
        var samples = new[]
        {
            Sample(0, ias: 240, flaps: 5),
            Sample(5, ias: 260, flaps: 5),
            Sample(10, ias: 262, flaps: 5),
            Sample(15, ias: 240, flaps: 5),
            Sample(20, ias: 270, flaps: 1),
            Sample(25, ias: 255, flaps: 5),
            Sample(30, ias: 245, flaps: 5)
        };

        var result = Evaluate(Flight() with { Samples = samples },
            Points(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, -20, ScoringMode.PerOccurrence, max: 250, text: "5"));

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(-40, item.Points);
    }

    [Fact]
    public void Flaps_overspeed_respects_the_grace_period()
    {
        var samples = new[]
        {
            Sample(0, ias: 260, flaps: 5),
            Sample(3, ias: 260, flaps: 5),
            Sample(4, ias: 240, flaps: 5)
        };

        var result = Evaluate(Flight() with { Samples = samples },
            Reject(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, max: 250, text: "5", grace: 5));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Samples_are_evaluated_in_time_order()
    {
        var samples = new[]
        {
            Sample(10, ias: 260, flaps: 5),
            Sample(0, ias: 260, flaps: 5),
            Sample(20, ias: 240, flaps: 5)
        };

        var result = Evaluate(Flight() with { Samples = samples },
            Reject(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, max: 250, text: "5", grace: 10));

        Assert.True(result.Rejected);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(2, false)]
    public void Takeoff_flaps_set_and_not_set(int flaps, bool valid)
    {
        var result = Evaluate(Flight() with { TakeoffFlaps = flaps },
            Points(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsSet, 5, text: "1,5,10,15,25"),
            Reject(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsNotSet, text: "1,5,10,15,25"));

        Assert.Equal(valid, !result.Rejected);
        Assert.Equal(valid ? 5 : 0, result.Points);
    }

    [Theory]
    [InlineData("B738", 40, false)]
    [InlineData("B738", 25, true)]
    [InlineData("B77W", 25, false)]
    [InlineData("B77W", 40, true)]
    public void Landing_flaps_depend_on_the_aircraft_type(string type, int flaps, bool rejected)
    {
        var result = Evaluate(Flight(type) with { LandingFlaps = flaps },
            Reject(ScoringCategory.Configuration, ScoringCheck.LandingFlapsNotSet, text: "30,40", types: "B738"),
            Reject(ScoringCategory.Configuration, ScoringCheck.LandingFlapsNotSet, text: "25,30", types: "B77W,B77F"));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData("B738", 10.9, false)]
    [InlineData("B738", 11.0, true)]
    [InlineData("B77W", 10.0, true)]
    [InlineData("B77W", 9.9, false)]
    [InlineData("B77F", 12.0, false)]
    [InlineData("B77F", 12.1, true)]
    public void Tail_strike_uses_the_pitch_of_each_type(string type, double pitch, bool rejected)
    {
        var result = Evaluate(Flight(type) with { LiftoffPitch = (decimal)pitch },
            Reject(ScoringCategory.Takeoff, ScoringCheck.TailStrike, min: 11, types: "B738,B739,B38M"),
            Reject(ScoringCategory.Takeoff, ScoringCheck.TailStrike, min: 10, types: "B77W"),
            Reject(ScoringCategory.Takeoff, ScoringCheck.TailStrike, min: 12.1m, types: "B77F"));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData(150, 10, false)]
    [InlineData(192, 10, false)]
    [InlineData(193, 0, true)]
    public void Rotation_speed_above_rejects_and_within_rewards(int vr, int points, bool rejected)
    {
        var result = Evaluate(Flight() with { RotationSpeedKt = vr },
            Reject(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedAbove, max: 192),
            Points(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedWithin, 10, max: 192));

        Assert.Equal(points, result.Points);
        Assert.Equal(rejected, result.Rejected);
    }

    [Fact]
    public void Gear_overspeed_counts_only_the_transition_in_that_direction()
    {
        var samples = new[]
        {
            Sample(0, ias: 200, gearDown: true),
            Sample(5, ias: 240, gearDown: false),
            Sample(10, ias: 280, gearDown: false),
            Sample(15, ias: 275, gearDown: true)
        };

        var result = Evaluate(Flight() with { Samples = samples },
            Points(ScoringCategory.LandingGear, ScoringCheck.GearUpOverspeed, -10, ScoringMode.PerOccurrence, max: 235),
            Points(ScoringCategory.LandingGear, ScoringCheck.GearDownOverspeed, -20, ScoringMode.PerOccurrence, max: 270));

        Assert.Equal(-30, result.Points);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void Gear_moving_below_the_limit_is_fine()
    {
        var samples = new[] { Sample(0, ias: 160, gearDown: true), Sample(5, ias: 170, gearDown: false) };

        var result = Evaluate(Flight() with { Samples = samples },
            Reject(ScoringCategory.LandingGear, ScoringCheck.GearUpOverspeed, max: 235));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Overspeed_per_second_discounts_the_grace_period()
    {
        var events = new[] { Event(FlightEventType.Overspeed, 12), Event(FlightEventType.Overspeed, 3, 60) };

        var result = Evaluate(Flight() with { Events = events },
            Points(ScoringCategory.Overspeed, ScoringCheck.Overspeed, -10, ScoringMode.PerSecond, grace: 5));

        var item = Assert.Single(result.Items);
        Assert.Equal(7, item.Quantity);
        Assert.Equal(-70, item.Points);
    }

    [Fact]
    public void Overspeed_total_rejects_when_the_penalty_reaches_the_limit()
    {
        var events = new[] { Event(FlightEventType.Overspeed, 35) };

        var result = Evaluate(Flight() with { Events = events },
            Points(ScoringCategory.Overspeed, ScoringCheck.Overspeed, -10, ScoringMode.PerSecond, grace: 5),
            Reject(ScoringCategory.Rejection, ScoringCheck.OverspeedTotal, min: 300));

        Assert.True(result.Rejected);
    }

    [Fact]
    public void Overspeed_total_stays_quiet_below_the_limit()
    {
        var events = new[] { Event(FlightEventType.Overspeed, 20) };

        var result = Evaluate(Flight() with { Events = events },
            Points(ScoringCategory.Overspeed, ScoringCheck.Overspeed, -10, ScoringMode.PerSecond, grace: 5),
            Reject(ScoringCategory.Rejection, ScoringCheck.OverspeedTotal, min: 300));

        Assert.False(result.Rejected);
        Assert.Equal(-150, result.Points);
    }

    [Theory]
    [InlineData(FlightEventType.Slew, ScoringCheck.Slew)]
    [InlineData(FlightEventType.Stall, ScoringCheck.Stall)]
    [InlineData(FlightEventType.Pause, ScoringCheck.Pause)]
    [InlineData(FlightEventType.SimulationRate, ScoringCheck.SimulationRate)]
    [InlineData(FlightEventType.Refuel, ScoringCheck.Refuel)]
    public void Simulator_events_are_penalized_once(FlightEventType type, ScoringCheck check)
    {
        var events = new[] { Event(type, 40), Event(type, 50, 100) };

        var result = Evaluate(Flight() with { Events = events },
            Points(ScoringCategory.Simulator, check, -100, grace: 30));

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(-100, item.Points);
    }

    [Fact]
    public void Short_pauses_inside_the_grace_period_are_ignored()
    {
        var events = new[] { Event(FlightEventType.Pause, 20) };

        var result = Evaluate(Flight() with { Events = events },
            Points(ScoringCategory.Simulator, ScoringCheck.Pause, -50, grace: 30));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void A_crash_rejects_the_flight()
    {
        var result = Evaluate(Flight() with { Events = [Event(FlightEventType.Crash, 0)] },
            Reject(ScoringCategory.Rejection, ScoringCheck.Crash));

        Assert.True(result.Rejected);
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(1, 5)]
    public void Engine_start_order(int firstEngine, int expected)
    {
        var result = Evaluate(Flight() with { FirstEngineStarted = firstEngine },
            Points(ScoringCategory.Procedures, ScoringCheck.EngineStartOrder, 10, min: 2),
            Points(ScoringCategory.Procedures, ScoringCheck.EngineStartOrder, 5, min: 1));

        Assert.Equal(expected, result.Points);
    }

    [Theory]
    [InlineData("SBFL", 200, 10)]
    [InlineData("SBFL", 100, -5)]
    [InlineData("SBSP", 100, 0)]
    [InlineData("sbrj", 200, 0)]
    public void Taxi_in_time_skips_the_excluded_airports(string arrival, int seconds, int expected)
    {
        var data = Flight() with { ArrivalIcao = arrival.ToUpperInvariant(), PlannedArrivalIcao = arrival.ToUpperInvariant(), TaxiInSeconds = seconds };

        var result = Evaluate(data,
            Points(ScoringCategory.Procedures, ScoringCheck.TaxiInAbove, 10, min: 150, text: "SBRJ,SBSP"),
            Points(ScoringCategory.Procedures, ScoringCheck.TaxiInBelow, -5, min: 150, text: "SBRJ,SBSP"));

        Assert.Equal(expected, result.Points);
    }

    [Theory]
    [InlineData(false, 10)]
    [InlineData(true, -50)]
    public void Engines_state_when_sending_the_log(bool running, int expected)
    {
        var result = Evaluate(Flight() with { EnginesRunningAtLog = running },
            Points(ScoringCategory.Procedures, ScoringCheck.EnginesOffAtLog, 10),
            Points(ScoringCategory.Procedures, ScoringCheck.EnginesRunningAtLog, -50));

        Assert.Equal(expected, result.Points);
    }

    [Fact]
    public void Unknown_engine_state_scores_nothing()
    {
        var result = Evaluate(Flight(),
            Points(ScoringCategory.Procedures, ScoringCheck.EnginesOffAtLog, 10),
            Points(ScoringCategory.Procedures, ScoringCheck.EnginesRunningAtLog, -50));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Taxi_speed_only_counts_taxi_phases_below_takeoff_roll()
    {
        var samples = new[]
        {
            Sample(0, groundSpeed: 20, phase: "Taxi out"),
            Sample(5, groundSpeed: 35, phase: "Taxi out"),
            Sample(10, groundSpeed: 25, phase: "Taxi out"),
            Sample(15, groundSpeed: 120, phase: "Takeoff"),
            Sample(20, groundSpeed: 70, phase: "taxi in"),
            Sample(25, groundSpeed: 32, phase: "taxi in"),
            Sample(30, groundSpeed: 15, phase: "taxi in")
        };

        var result = Evaluate(Flight() with { Samples = samples },
            Points(ScoringCategory.Procedures, ScoringCheck.TaxiSpeed, -50, ScoringMode.PerOccurrence, max: 30));

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.Quantity);
    }

    [Theory]
    [InlineData(FlightNetwork.Offline, true)]
    [InlineData(FlightNetwork.Vatsim, false)]
    [InlineData(FlightNetwork.Ivao, false)]
    public void Offline_flights_are_rejected(FlightNetwork network, bool rejected)
    {
        var result = Evaluate(Flight(network: network), Reject(ScoringCategory.Rejection, ScoringCheck.Offline));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData(FlightNetwork.Vatsim, 70, true)]
    [InlineData(FlightNetwork.Vatsim, 80, false)]
    [InlineData(FlightNetwork.Offline, 0, false)]
    public void Low_online_percent_is_only_checked_on_a_network(FlightNetwork network, int percent, bool rejected)
    {
        var result = Evaluate(Flight(network: network) with { OnlinePercent = percent },
            Reject(ScoringCategory.Rejection, ScoringCheck.LowOnlinePercent, min: 80));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData(700, true)]
    [InlineData(800, false)]
    public void Early_flap_retraction(int height, bool rejected)
    {
        var result = Evaluate(Flight() with { FlapsRetractedAtFt = height },
            Reject(ScoringCategory.Rejection, ScoringCheck.EarlyFlapRetraction, max: 800));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData(900, true)]
    [InlineData(1000, false)]
    [InlineData(1500, false)]
    public void Unstable_approach(int configuredAt, bool rejected)
    {
        var result = Evaluate(Flight() with { LandingConfiguredAtFt = configuredAt },
            Reject(ScoringCategory.Rejection, ScoringCheck.UnstableApproach, min: 1000));

        Assert.Equal(rejected, result.Rejected);
    }

    [Theory]
    [InlineData("B738", 4300, true)]
    [InlineData("B738", 4400, false)]
    [InlineData("B77W", 8000, true)]
    [InlineData("B77W", 12000, false)]
    public void Minimum_fuel_at_pushback_depends_on_the_type(string type, int fuel, bool rejected)
    {
        var result = Evaluate(Flight(type) with { FuelAtPushbackKg = fuel },
            Reject(ScoringCategory.Rejection, ScoringCheck.LowFuelAtPushback, min: 4400, types: "B738"),
            Reject(ScoringCategory.Rejection, ScoringCheck.LowFuelAtPushback, min: 12000, types: "B77W,B77F"));

        Assert.Equal(rejected, result.Rejected);
    }

    [Fact]
    public void Fuel_in_pounds_is_rejected()
    {
        var result = Evaluate(Flight() with { FuelInPounds = true }, Reject(ScoringCategory.Rejection, ScoringCheck.FuelInPounds));

        Assert.True(result.Rejected);
    }

    [Fact]
    public void An_unreported_diversion_is_rejected()
    {
        var data = Flight() with { ArrivalIcao = "SBKP" };

        Assert.True(Evaluate(data, Reject(ScoringCategory.Rejection, ScoringCheck.UnreportedDiversion)).Rejected);
        Assert.False(Evaluate(data with { DiversionReported = true }, Reject(ScoringCategory.Rejection, ScoringCheck.UnreportedDiversion)).Rejected);
    }

    [Fact]
    public void Incomplete_data_is_penalized()
    {
        var result = Evaluate(Flight() with { IncompleteData = true },
            Points(ScoringCategory.FlightData, ScoringCheck.IncompleteData, -300));

        Assert.Equal(-300, result.Points);
        Assert.Equal(70, result.Conformity);
    }

    [Fact]
    public void A_typical_good_flight_adds_up()
    {
        var data = Flight() with
        {
            BlockMinutes = 75,
            LandingRateFpm = -130,
            TakeoffFlaps = 5,
            LandingFlaps = 30,
            RotationSpeedKt = 150,
            FirstEngineStarted = 2,
            EnginesRunningAtLog = false,
            FuelAtPushbackKg = 6500,
            OnlinePercent = 100
        };

        var result = Evaluate(data,
            Points(ScoringCategory.FlightTime, ScoringCheck.BlockTime, 30, min: 60, max: 119),
            Points(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, 60, min: 101, max: 150),
            Points(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsSet, 5, text: "1,5,10,15,25"),
            Points(ScoringCategory.Configuration, ScoringCheck.LandingFlapsSet, 5, text: "30,40"),
            Points(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedWithin, 10, max: 192),
            Points(ScoringCategory.Procedures, ScoringCheck.EngineStartOrder, 10, min: 2),
            Points(ScoringCategory.Procedures, ScoringCheck.EnginesOffAtLog, 10),
            Reject(ScoringCategory.Rejection, ScoringCheck.LowFuelAtPushback, min: 4400),
            Reject(ScoringCategory.Rejection, ScoringCheck.LowOnlinePercent, min: 80),
            Reject(ScoringCategory.Rejection, ScoringCheck.Offline));

        Assert.Equal(130, result.Points);
        Assert.False(result.Rejected);
        Assert.Equal(100, result.Conformity);
        Assert.Equal(7, result.Items.Count);
    }
}