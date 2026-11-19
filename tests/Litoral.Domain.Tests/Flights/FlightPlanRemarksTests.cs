using Litoral.Domain.Flights;

namespace Litoral.Domain.Tests.Flights;

public class FlightPlanRemarksTests
{
    private const string SimBrief = "PBN/A1B1C1D1S1S2 DOF/261119 REG/PRLAB EET/SBBS0101 SEL/ERAP OPR/LTL PER/D";

    [Fact]
    public void The_required_remarks_use_the_pilot_id_on_the_site() =>
        Assert.Equal("OPR/LITORAL PIC/FO1000 CS/LITORAL", FlightPlanRemarks.Required(" fo1000 "));

    [Fact]
    public void The_simbrief_operator_is_replaced_by_the_litoral_remarks()
    {
        var result = FlightPlanRemarks.Apply(SimBrief, "FO1000");

        Assert.Equal("PBN/A1B1C1D1S1S2 DOF/261119 REG/PRLAB EET/SBBS0101 SEL/ERAP OPR/LITORAL PIC/FO1000 CS/LITORAL PER/D", result);
    }

    [Fact]
    public void The_litoral_remarks_go_before_rmk_when_there_is_no_per()
    {
        var result = FlightPlanRemarks.Apply("PBN/D2 DOF/261119 RMK/TCAS SIMBRIEF", "CMD1002");

        Assert.Equal("PBN/D2 DOF/261119 OPR/LITORAL PIC/CMD1002 CS/LITORAL RMK/TCAS SIMBRIEF", result);
    }

    [Fact]
    public void The_litoral_remarks_go_at_the_end_when_nothing_comes_after_them() =>
        Assert.Equal("PBN/D2 DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL", FlightPlanRemarks.Apply("PBN/D2 DOF/261119", "FO1000"));

    [Fact]
    public void Old_pic_and_cs_values_are_replaced_even_inside_the_remarks()
    {
        var result = FlightPlanRemarks.Apply("DOF/261119 OPR/LITORAL PIC/ALEXANDRE FAGUNDES CS/LTL RMK/TCAS", "FO1000");

        Assert.Equal("DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    public void An_empty_field_gets_only_the_litoral_remarks(string? input) =>
        Assert.Equal("OPR/LITORAL PIC/FO1000 CS/LITORAL", FlightPlanRemarks.Apply(input, "FO1000"));

    [Fact]
    public void Free_text_without_an_indicator_is_kept_as_rmk() =>
        Assert.Equal("OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS", FlightPlanRemarks.Apply("tcas", "FO1000"));

    [Fact]
    public void Extra_spaces_and_lower_case_are_cleaned() =>
        Assert.Equal("DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS",
            FlightPlanRemarks.Apply("  dof/261119    rmk/tcas  ", "FO1000"));

    [Fact]
    public void Applying_twice_gives_the_same_result()
    {
        var once = FlightPlanRemarks.Apply(SimBrief, "FO1000");

        Assert.Equal(once, FlightPlanRemarks.Apply(once, "FO1000"));
    }

    [Fact]
    public void Compliant_remarks_have_all_three_values()
    {
        Assert.True(FlightPlanRemarks.IsCompliant(FlightPlanRemarks.Apply(SimBrief, "FO1000"), "FO1000"));
        Assert.False(FlightPlanRemarks.IsCompliant(SimBrief, "FO1000"));
        Assert.False(FlightPlanRemarks.IsCompliant("OPR/LITORAL PIC/FO1000", "FO1000"));
        Assert.False(FlightPlanRemarks.IsCompliant("OPR/LITORAL PIC/FO1001 CS/LITORAL", "FO1000"));
        Assert.False(FlightPlanRemarks.IsCompliant(null, "FO1000"));
    }

    [Fact]
    public void The_icao_message_gets_the_remarks_in_item_18()
    {
        const string message = "(FPL-LTL1007-IS -B738/M-SDE2E3FGHIRWXY/LB1 -SBPA2135 -N0455F390 ARTIL UZ104 SAVLU -SBGR0125 SBKP -PBN/A1B1C1D1S1S2 DOF/261119 OPR/LTL RMK/TCAS SIMBRIEF)";

        var result = FlightPlanRemarks.ApplyToIcaoMessage(message, "FO1000");

        Assert.Equal("(FPL-LTL1007-IS -B738/M-SDE2E3FGHIRWXY/LB1 -SBPA2135 -N0455F390 ARTIL UZ104 SAVLU -SBGR0125 SBKP -PBN/A1B1C1D1S1S2 DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS SIMBRIEF)", result);
    }

    [Fact]
    public void Item_19_after_item_18_is_kept()
    {
        const string message = "(FPL-LTL1007-IS -B738/M-S/C -SBPA2135 -N0455F390 DCT -SBGR0125 -DOF/261119 -E/0249 P/113)";

        var result = FlightPlanRemarks.ApplyToIcaoMessage(message, "FO1000");

        Assert.Equal("(FPL-LTL1007-IS -B738/M-S/C -SBPA2135 -N0455F390 DCT -SBGR0125 -DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL -E/0249 P/113)", result);
    }

    [Fact]
    public void An_empty_item_18_is_filled()
    {
        const string message = "(FPL-LTL1007-IS -B738/M-S/C -SBPA2135 -N0455F390 DCT -SBGR0125 -0)";

        Assert.Equal("(FPL-LTL1007-IS -B738/M-S/C -SBPA2135 -N0455F390 DCT -SBGR0125 -OPR/LITORAL PIC/FO1000 CS/LITORAL)",
            FlightPlanRemarks.ApplyToIcaoMessage(message, "FO1000"));
    }

    [Fact]
    public void The_vatsim_prefile_link_is_rewritten()
    {
        const string link = "https://my.vatsim.net/pilots/flightplan?altitude=39000&fuel_time=0249&raw=%28FPL-LTL1007-IS%20-B738%2FM-SDE2E3FGHIRWXY%2FLB1%20-SBPA2135%20-N0455F390%20ARTIL%20-SBGR0125%20SBKP%20-PBN%2FA1B1C1D1S1S2%20OPR%2FLTL%20RMK%2FTCAS%29";

        var result = FlightPlanRemarks.ApplyToPrefileLink(link, "FO1000")!;
        var raw = Uri.UnescapeDataString(result[(result.IndexOf("raw=", StringComparison.Ordinal) + 4)..]);

        Assert.StartsWith("https://my.vatsim.net/pilots/flightplan?altitude=39000&fuel_time=0249&raw=", result);
        Assert.Equal("(FPL-LTL1007-IS -B738/M-SDE2E3FGHIRWXY/LB1 -SBPA2135 -N0455F390 ARTIL -SBGR0125 SBKP -PBN/A1B1C1D1S1S2 OPR/LITORAL PIC/FO1000 CS/LITORAL RMK/TCAS)", raw);
    }

    [Fact]
    public void A_link_with_item_18_in_its_own_parameter_is_rewritten()
    {
        const string link = "https://fpl.ivao.aero/flight-plans/new?callsign=LTL1007&departureId=SBPA&other=PBN%2FA1B1+DOF%2F261119+OPR%2FLTL&endurance=0249";

        var result = FlightPlanRemarks.ApplyToPrefileLink(link, "FO1000")!;

        Assert.Contains("callsign=LTL1007&departureId=SBPA&other=", result);
        Assert.Contains("&endurance=0249", result);
        Assert.Contains(Uri.EscapeDataString("PBN/A1B1 DOF/261119 OPR/LITORAL PIC/FO1000 CS/LITORAL"), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("https://fpl.ivao.aero/")]
    [InlineData("https://my.vatsim.net/pilots/flightplan?callsign=LTL1007")]
    public void Links_without_item_18_are_left_alone(string? link) =>
        Assert.Equal(link, FlightPlanRemarks.ApplyToPrefileLink(link, "FO1000"));
}