using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Award : Entity
{
    public const int MaxNameLength = 80;
    public const int MaxDescriptionLength = 400;

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public AwardKind Kind { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Award() { }

    public Award(string name, string description, AwardKind kind)
    {
        Kind = kind;
        Update(name, description, true);
    }

    public void Update(string name, string description, bool isActive)
    {
        var n = name?.Trim() ?? "";
        var d = description?.Trim() ?? "";

        if (n.Length == 0)
            throw new DomainException("The award name is required.");
        if (n.Length > MaxNameLength)
            throw new DomainException($"The award name must have up to {MaxNameLength} characters.");
        if (d.Length == 0)
            throw new DomainException("The award description is required.");
        if (d.Length > MaxDescriptionLength)
            throw new DomainException($"The award description must have up to {MaxDescriptionLength} characters.");

        Name = n;
        Description = d;
        IsActive = isActive;
    }

    public string? SetImage(string? url)
    {
        var previous = ImageUrl;
        ImageUrl = url;
        return previous;
    }
}