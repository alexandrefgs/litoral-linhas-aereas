using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Tests.Flight;

public class GeoTests
{
    [Fact]
    public void Distance_between_sbgr_and_sbfl_is_about_278_nm() =>
        Assert.InRange(Geo.DistanceNm(TestFlights.SbgrLat, TestFlights.SbgrLon, TestFlights.SbflLat, TestFlights.SbflLon), 275, 281);

    [Fact]
    public void The_same_point_is_zero_nm() =>
        Assert.Equal(0, Geo.DistanceNm(-23.4, -46.4, -23.4, -46.4));

    [Fact]
    public void One_degree_of_latitude_is_60_nm() =>
        Assert.InRange(Geo.DistanceNm(0, 0, 1, 0), 59.9, 60.1);
}

public class EpisodeTrackerTests
{
    private static readonly DateTime T0 = TestFlights.Start;

    [Fact]
    public void An_episode_reports_its_start_and_duration_when_it_ends()
    {
        var tracker = new EpisodeTracker("Pause");

        Assert.Null(tracker.Update(true, T0));
        Assert.Null(tracker.Update(true, T0.AddSeconds(10)));
        var report = tracker.Update(false, T0.AddSeconds(25));

        Assert.NotNull(report);
        Assert.Equal("Pause", report!.Type);
        Assert.Equal(T0, report.At);
        Assert.Equal(25, report.DurationSeconds);
        Assert.False(tracker.IsActive);
    }

    [Fact]
    public void Nothing_is_reported_while_inactive()
    {
        var tracker = new EpisodeTracker("Stall");

        Assert.Null(tracker.Update(false, T0));
        Assert.Null(tracker.Close(T0));
    }

    [Fact]
    public void The_value_kept_is_the_largest_seen()
    {
        var tracker = new EpisodeTracker("SimulationRate");

        tracker.Update(true, T0, 2m);
        tracker.Update(true, T0.AddSeconds(5), 4m);
        tracker.Update(true, T0.AddSeconds(9), 2m);
        var report = tracker.Close(T0.AddSeconds(10));

        Assert.Equal(4m, report!.Value);
    }

    [Fact]
    public void A_new_episode_starts_clean_after_one_ends()
    {
        var tracker = new EpisodeTracker("Overspeed");
        tracker.Update(true, T0, 1m);
        tracker.Update(false, T0.AddSeconds(3));

        tracker.Update(true, T0.AddSeconds(60));
        var second = tracker.Close(T0.AddSeconds(62));

        Assert.Equal(T0.AddSeconds(60), second!.At);
        Assert.Equal(2, second.DurationSeconds);
        Assert.Null(second.Value);
    }
}