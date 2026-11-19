using Litoral.Api.IntegrationTests.Infrastructure;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Api.IntegrationTests.Scoring;

[Collection(ApiCollection.Name)]
public class RemarksRuleTests(LitoralApiFactory api)
{
    [Fact]
    public async Task The_remarks_rule_asks_for_operator_pilot_id_and_radio_callsign()
    {
        var rule = await api.WithDbAsync(db => db.ScoringRules.SingleAsync(r => r.Check == ScoringCheck.MissingRemarks));

        Assert.Equal(DbSeeder.RemarksRuleText, rule.Text);
        Assert.Equal(DbSeeder.RemarksRuleDescription, rule.Description);
        Assert.Equal(ScoringOutcome.Reject, rule.Outcome);
    }
}