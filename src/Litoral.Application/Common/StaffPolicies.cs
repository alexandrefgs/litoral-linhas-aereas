using Litoral.Domain.Staff;

namespace Litoral.Application.Common;

public static class StaffPolicies
{
    public const string ManageStaff = "ManageStaff";
    public const string ViewReports = "ViewReports";
    public const string ManagePilots = "ManagePilots";
    public const string ManageOperations = "ManageOperations";
    public const string ManageFleet = "ManageFleet";
    public const string ManageRoutes = "ManageRoutes";
    public const string ReviewPireps = "ReviewPireps";
    public const string ManageDownloads = "ManageDownloads";

    public static readonly IReadOnlyDictionary<string, string[]> Positions = new Dictionary<string, string[]>
    {
        [ManageStaff] = [StaffPositions.Chairman, StaffPositions.ViceChairman],
        [ViewReports] = [StaffPositions.GeneralDirector],
        [ManagePilots] = [StaffPositions.HeadOfHr],
        [ManageOperations] = [StaffPositions.DirectorOfOperations],
        [ManageFleet] = [StaffPositions.FleetManager],
        [ManageRoutes] = [StaffPositions.RouteManager],
        [ReviewPireps] = [StaffPositions.SafetyManager],
        [ManageDownloads] = [StaffPositions.FleetManager, StaffPositions.DirectorOfOperations]
    };
}