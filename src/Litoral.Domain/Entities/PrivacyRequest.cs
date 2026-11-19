using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class PrivacyRequest : Entity
{
    public const int MaxMessageLength = 2000;
    public const int MaxResponseLength = 2000;
    public const int MaxNameLength = 150;
    public const int ResponseDays = 15;

    public Guid? PilotId { get; private set; }
    public Pilot? Pilot { get; private set; }
    public string PilotLabel { get; private set; } = null!;
    public PrivacyRequestType Type { get; private set; }
    public string Message { get; private set; } = null!;
    public PrivacyRequestStatus Status { get; private set; } = PrivacyRequestStatus.Open;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime DueAt { get; private set; }
    public string? Response { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }

    public bool IsOpen => Status == PrivacyRequestStatus.Open;

    private PrivacyRequest() { }

    public PrivacyRequest(Guid pilotId, string pilotLabel, PrivacyRequestType type, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new DomainException("Describe your request.");
        if (message.Trim().Length > MaxMessageLength) throw new DomainException($"The request can have at most {MaxMessageLength} characters.");

        PilotId = pilotId;
        PilotLabel = Cut(pilotLabel, MaxNameLength);
        Type = type;
        Message = message.Trim();
        DueAt = CreatedAt.AddDays(ResponseDays);
    }

    public void Resolve(PrivacyRequestStatus status, string response, string resolvedBy)
    {
        if (!IsOpen) throw new DomainException("This request was already answered.");
        if (status == PrivacyRequestStatus.Open) throw new DomainException("Choose completed or rejected.");
        if (string.IsNullOrWhiteSpace(response)) throw new DomainException("Write a response to the pilot.");
        if (response.Trim().Length > MaxResponseLength) throw new DomainException($"The response can have at most {MaxResponseLength} characters.");

        Status = status;
        Response = response.Trim();
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = Cut(resolvedBy, MaxNameLength);
    }

    private static string Cut(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}