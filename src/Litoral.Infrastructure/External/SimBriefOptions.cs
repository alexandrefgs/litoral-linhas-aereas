namespace Litoral.Infrastructure.External;

public sealed class SimBriefOptions
{
    public const string SectionName = "SimBrief";

    public string? ApiKey { get; init; }
    public string OutputPage { get; init; } = "http://localhost:4200/simbrief-return.html";
}