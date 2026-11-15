namespace Litoral.Domain.Staff;

public sealed record StaffPositionDefinition(string Code, string Name, StaffTeam Team, bool SingleHolder);

public static class StaffPositions
{
    public const string CeoFounder = "CEO_FOUNDER";
    public const string Chairman = "CHAIRMAN";
    public const string ViceChairman = "VICE_CHAIRMAN";
    public const string GeneralDirector = "GENERAL_DIRECTOR";
    public const string HeadOfHr = "HEAD_OF_HR";
    public const string DirectorOfOperations = "DIRECTOR_OF_OPERATIONS";
    public const string FleetManager = "FLEET_MANAGER";
    public const string RouteManager = "ROUTE_MANAGER";
    public const string SafetyManager = "SAFETY_MANAGER";

    public static readonly IReadOnlyList<StaffPositionDefinition> All =
    [
        new(CeoFounder, "CEO & Founder", StaffTeam.Executive, true),
        new(Chairman, "Chairman", StaffTeam.Executive, true),
        new(ViceChairman, "Vice Chairman", StaffTeam.Executive, true),
        new(GeneralDirector, "General Director", StaffTeam.Executive, true),
        new(HeadOfHr, "Head of HR", StaffTeam.Operations, true),
        new(DirectorOfOperations, "Director of Operations", StaffTeam.Operations, true),
        new(FleetManager, "Fleet Manager", StaffTeam.Operations, true),
        new(RouteManager, "Route Manager", StaffTeam.Operations, true),
        new(SafetyManager, "Safety Manager", StaffTeam.Safety, false)
    ];

    public static StaffPositionDefinition? Find(string code) =>
        All.FirstOrDefault(p => p.Code == code);
}