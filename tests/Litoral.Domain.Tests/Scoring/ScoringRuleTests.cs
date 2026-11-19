using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Scoring;

namespace Litoral.Domain.Tests.Scoring;

public class ScoringRuleTests
{
    [Fact]
    public void Rejection_rules_never_carry_points()
    {
        var rule = new ScoringRule(ScoringCategory.Rejection, ScoringCheck.Crash, "Crash", ScoringOutcome.Reject, -500);

        Assert.Equal(0, rule.Points);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_is_required(string description)
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, description, ScoringOutcome.Points, -100));
    }

    [Fact]
    public void Description_is_limited_in_length()
    {
        var description = new string('x', ScoringRule.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, description, ScoringOutcome.Points, -100));
    }

    [Fact]
    public void A_mode_the_check_does_not_support_is_refused()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Pause, "Pause", ScoringOutcome.Points, -50, ScoringMode.PerSecond));
    }

    [Fact]
    public void Range_checks_need_at_least_one_limit()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Any", ScoringOutcome.Points, 10));
    }

    [Fact]
    public void Range_checks_refuse_a_lower_limit_above_the_upper_limit()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Wrong", ScoringOutcome.Points, 10, min: 120, max: 60));
    }

    [Fact]
    public void Single_limit_checks_need_that_limit()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Takeoff, ScoringCheck.TailStrike, "Tail strike", ScoringOutcome.Reject, 0));
    }

    [Fact]
    public void Checks_with_a_text_parameter_need_the_text()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps", ScoringOutcome.Reject, 0, max: 250));
    }

    [Fact]
    public void Limits_the_check_does_not_use_are_discarded()
    {
        var rule = new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100, min: 5, max: 10, text: "x");

        Assert.Null(rule.Min);
        Assert.Null(rule.Max);
        Assert.Null(rule.Text);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ScoringRule.MaxGraceSeconds + 1)]
    public void Grace_period_must_be_within_bounds(int grace)
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100, graceSeconds: grace));
    }

    [Fact]
    public void Points_must_be_within_bounds()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, ScoringRule.MaxPoints + 1));
    }

    [Fact]
    public void Aircraft_types_are_normalized()
    {
        var rule = new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100,
            aircraftTypes: " b738; b77w b738 ");

        Assert.Equal("B738,B77W", rule.AircraftTypes);
        Assert.Equal(new[] { "B738", "B77W" }, rule.AircraftTypeList);
    }

    [Fact]
    public void Invalid_aircraft_types_are_refused()
    {
        Assert.Throws<DomainException>(() =>
            new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100, aircraftTypes: "BOEING777"));
    }

    [Fact]
    public void A_rule_without_types_applies_to_every_type()
    {
        var rule = new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100);

        Assert.True(rule.AppliesTo("B738"));
        Assert.True(rule.AppliesTo("B77W"));
    }

    [Fact]
    public void A_rule_with_types_applies_only_to_them()
    {
        var rule = new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100, aircraftTypes: "B77W,B77F");

        Assert.True(rule.AppliesTo("b77w"));
        Assert.False(rule.AppliesTo("B738"));
    }

    [Fact]
    public void Deactivate_and_activate_toggle_the_rule()
    {
        var rule = new ScoringRule(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew", ScoringOutcome.Points, -100);

        rule.Deactivate();
        Assert.False(rule.IsActive);

        rule.Activate();
        Assert.True(rule.IsActive);
    }
}