using Litoral.Domain.Scoring;

namespace Litoral.Domain.Tests.Scoring;

public class EvaluationResultTests
{
    private static ScoreResultItem Item(int points, ScoringOutcome outcome = ScoringOutcome.Points) =>
        new(Guid.NewGuid(), ScoringCategory.Procedures, ScoringCheck.TaxiSpeed, "item", outcome, points, 1);

    [Fact]
    public void Conformity_is_100_without_items()
    {
        Assert.Equal(100, EvaluationResult.ConformityOf([]));
    }

    [Theory]
    [InlineData(-9, 100)]
    [InlineData(-10, 99)]
    [InlineData(-50, 95)]
    [InlineData(-155, 85)]
    [InlineData(-2000, 0)]
    public void Conformity_drops_one_percent_per_ten_penalty_points(int penalty, int expected)
    {
        Assert.Equal(expected, EvaluationResult.ConformityOf([Item(penalty)]));
    }

    [Fact]
    public void Bonuses_do_not_offset_penalties_in_the_conformity()
    {
        Assert.Equal(95, EvaluationResult.ConformityOf([Item(-50), Item(100)]));
    }

    [Fact]
    public void A_rejection_item_sets_the_conformity_to_zero()
    {
        Assert.Equal(0, EvaluationResult.ConformityOf([Item(100), Item(0, ScoringOutcome.Reject)]));
    }

    [Fact]
    public void From_sums_the_points_and_flags_the_rejection()
    {
        var result = EvaluationResult.From([Item(60), Item(-50), Item(0, ScoringOutcome.Reject)]);

        Assert.Equal(10, result.Points);
        Assert.True(result.Rejected);
        Assert.Equal(0, result.Conformity);
        Assert.Equal(3, result.Items.Count);
    }
}