using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class PilotApplication : Entity
{
    public const int MaxNameLength = 60;
    public const int MinMotivationLength = 30;
    public const int MaxMotivationLength = 1000;
    public const int MaxAddOnsLength = 200;

    public static readonly IReadOnlyList<string> Simulators = ["MSFS 2020", "MSFS 2024", "X-Plane 11", "X-Plane 12", "Prepar3D"];
    public static readonly IReadOnlyList<string> AddOnOptions = ["PMDG 737", "iFly 737 MAX 8", "Zibo 737-800", "LevelUp 737NG", "Other"];

    public string UserId { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string HubIcao { get; private set; } = null!;
    public string? VatsimId { get; private set; }
    public string? IvaoId { get; private set; }
    public string Simulator { get; private set; } = null!;
    public string AddOns { get; private set; } = null!;
    public string Motivation { get; private set; } = null!;
    public string AcceptedTermsVersion { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<string> AddOnList => AddOns.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private PilotApplication() { }

    public PilotApplication(
        string userId,
        string firstName,
        string lastName,
        string hubIcao,
        string? vatsimId,
        string? ivaoId,
        string? simulator,
        IEnumerable<string>? addOns,
        string? motivation,
        string termsVersion)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("First and last name are required.");
        if (firstName.Trim().Length > MaxNameLength || lastName.Trim().Length > MaxNameLength)
            throw new DomainException($"First and last name must have up to {MaxNameLength} characters.");

        var vatsim = NetworkId(vatsimId, "VATSIM ID");
        var ivao = NetworkId(ivaoId, "IVAO ID");
        if (vatsim is null && ivao is null)
            throw new DomainException("Enter your VATSIM ID, your IVAO ID or both.");

        var sim = Simulators.FirstOrDefault(s => s == simulator?.Trim())
            ?? throw new DomainException("Choose your simulator.");

        var chosen = (addOns ?? []).Select(a => a.Trim()).Where(AddOnOptions.Contains).Distinct().ToList();
        if (chosen.Count == 0)
            throw new DomainException("Choose the 737 add-ons you have.");

        var text = motivation?.Trim() ?? "";
        if (text.Length < MinMotivationLength || text.Length > MaxMotivationLength)
            throw new DomainException($"Tell us about your experience in {MinMotivationLength} to {MaxMotivationLength} characters.");

        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        HubIcao = hubIcao.ToUpperInvariant();
        VatsimId = vatsim;
        IvaoId = ivao;
        Simulator = sim;
        AddOns = string.Join(',', chosen);
        Motivation = text;
        AcceptedTermsVersion = termsVersion;
    }

    private static string? NetworkId(string? value, string label)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (clean is null) return null;
        if (!clean.All(char.IsAsciiDigit) || clean.Length < 4 || clean.Length > 10)
            throw new DomainException($"The {label} must have 4 to 10 digits.");
        return clean;
    }
}