using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class PilotAward : Entity
{
    public const int MaxNoteLength = 300;

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public Guid AwardId { get; private set; }
    public Award Award { get; private set; } = null!;
    public string? Note { get; private set; }
    public string GrantedBy { get; private set; } = null!;
    public DateTime GrantedAt { get; private set; } = DateTime.UtcNow;

    private PilotAward() { }

    public PilotAward(Pilot pilot, Award award, string? note, string grantedBy)
    {
        var n = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (n?.Length > MaxNoteLength)
            throw new DomainException($"The note must have up to {MaxNoteLength} characters.");

        Pilot = pilot;
        PilotId = pilot.Id;
        Award = award;
        AwardId = award.Id;
        Note = n;
        GrantedBy = grantedBy;
    }
}