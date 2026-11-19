using Litoral.Domain.Scoring;

namespace Litoral.Domain.Tests.Scoring;

public class ScoringCatalogTests
{
    [Fact]
    public void Every_check_has_a_catalog_entry()
    {
        foreach (var check in Enum.GetValues<ScoringCheck>())
            Assert.Equal(check, ScoringCatalog.Get(check).Check);
    }

    [Fact]
    public void Every_category_has_a_catalog_entry()
    {
        foreach (var category in Enum.GetValues<ScoringCategory>())
            Assert.Equal(category, ScoringCatalog.Get(category).Category);
    }

    [Fact]
    public void Every_check_allows_at_least_one_mode()
    {
        Assert.All(ScoringCatalog.Checks, c => Assert.NotEmpty(c.Modes));
    }

    [Fact]
    public void Catalog_labels_do_not_mention_the_manufacturer_manuals()
    {
        Assert.All(ScoringCatalog.Checks, c => Assert.DoesNotContain("FCOM", c.Label));
    }
}