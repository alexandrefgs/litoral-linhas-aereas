using System.Text.Json;
using Litoral.Domain.Flights;

namespace Litoral.Domain.Tests.Flights;

public class SimBriefDispatchTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 20, 52, 13, DateTimeKind.Utc);

    private static SimBriefFlight Flight(string type = "B738", string typeIcao = "B738", int minutes = 85) =>
        new("LTL", 1009, "LTL1009", type, typeIcao, "PS-LAA", "SBFL", "SBPA", minutes, "CMD1001 - J. Silva", "FO1000");

    private static Dictionary<string, string> Query(SimBriefFlight flight, DateTime? now = null) =>
        SimBriefDispatch.Parameters(flight, now ?? Now).ToDictionary(p => p.Key, p => p.Value);

    [Theory]
    [InlineData("2026-11-19T20:52:13Z", "2026-11-19T21:40:00Z")]
    [InlineData("2026-11-19T20:50:00Z", "2026-11-19T21:35:00Z")]
    [InlineData("2026-11-19T23:30:01Z", "2026-11-20T00:20:00Z")]
    public void Departure_is_45_minutes_from_now_rounded_up_to_5_minutes(string now, string expected)
    {
        var departure = SimBriefDispatch.DepartureTime(DateTime.Parse(now, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

        Assert.Equal(DateTime.Parse(expected, null, System.Globalization.DateTimeStyles.AdjustToUniversal), departure);
        Assert.Equal(DateTimeKind.Utc, departure.Kind);
    }

    [Fact]
    public void The_flight_and_departure_are_filled_in()
    {
        var q = Query(Flight());

        Assert.Equal("LTL", q["airline"]);
        Assert.Equal("1009", q["fltnum"]);
        Assert.Equal("LTL1009", q["callsign"]);
        Assert.Equal("PS-LAA", q["reg"]);
        Assert.Equal("SBFL", q["orig"]);
        Assert.Equal("SBPA", q["dest"]);
        Assert.Equal("19NOV26", q["date"]);
        Assert.Equal("21", q["deph"]);
        Assert.Equal("40", q["depm"]);
        Assert.Equal("1", q["steh"]);
        Assert.Equal("25", q["stem"]);
        Assert.Equal("CMD1001 - J. Silva", q["cpt"]);
        Assert.Equal("KGS", q["units"]);
        Assert.Equal("lido", q["planformat"]);
    }

    [Fact]
    public void A_departure_after_midnight_uses_the_next_day()
    {
        var q = Query(Flight(), new DateTime(2026, 11, 19, 23, 40, 0, DateTimeKind.Utc));

        Assert.Equal("20NOV26", q["date"]);
        Assert.Equal("00", q["deph"]);
        Assert.Equal("25", q["depm"]);
    }

    [Fact]
    public void The_saved_airframe_id_is_sent_as_the_type()
    {
        var q = Query(Flight(type: "123456_1582090020"));

        Assert.Equal("123456_1582090020", q["type"]);
    }

    [Fact]
    public void Item_18_goes_in_acdata_with_tcas()
    {
        using var acdata = JsonDocument.Parse(Query(Flight())["acdata"]);

        Assert.Equal("OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS", acdata.RootElement.GetProperty("extrarmk").GetString());
    }

    [Fact]
    public void The_dispatcher_remark_repeats_item_18() =>
        Assert.Equal("LITORAL LINHAS AEREAS - ITEM 18: OPR/LITORAL PIC/FO1000 CS/LITORAL", Query(Flight())["manualrmk"]);

    [Theory]
    [InlineData("B738")]
    [InlineData("B38M")]
    [InlineData("B736")]
    [InlineData("A320")]
    public void Medium_aircraft_fly_cost_index_10(string type)
    {
        var q = Query(Flight(typeIcao: type));

        Assert.Equal("CI", q["cruise"]);
        Assert.Equal("10", q["civalue"]);
        Assert.Equal(10, SimBriefDispatch.DefaultCostIndex(type));
    }

    [Theory]
    [InlineData("B77W")]
    [InlineData("B77F")]
    [InlineData("b789")]
    [InlineData("A359")]
    public void Heavy_aircraft_leave_the_cost_index_to_simbrief(string type)
    {
        var q = Query(Flight(typeIcao: type));

        Assert.True(SimBriefDispatch.IsHeavy(type));
        Assert.Null(SimBriefDispatch.DefaultCostIndex(type));
        Assert.False(q.ContainsKey("civalue"));
        Assert.False(q.ContainsKey("cruise"));
    }

    [Fact]
    public void A_short_route_has_zero_hours()
    {
        var q = Query(Flight(minutes: 45));

        Assert.Equal("0", q["steh"]);
        Assert.Equal("45", q["stem"]);
    }

    [Fact]
    public void The_url_is_escaped()
    {
        var url = SimBriefDispatch.Url(Flight(), Now);

        Assert.StartsWith("https://dispatch.simbrief.com/options/custom?airline=LTL&fltnum=1009&", url);
        Assert.Contains("cpt=CMD1001%20-%20J.%20Silva", url);
        Assert.Contains("acdata=%7B%22extrarmk%22%3A%22OPR%2FLITORAL%20PIC%2FFO1000%20CS%2FLITORAL%20RMK%2FTCAS%22%7D", url);
    }
}