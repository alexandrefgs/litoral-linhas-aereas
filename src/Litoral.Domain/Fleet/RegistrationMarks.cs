namespace Litoral.Domain.Fleet;

public static class RegistrationMarks
{
    public static readonly IReadOnlyList<string> Nationalities = ["PP", "PR", "PS", "PT", "PU"];

    private static readonly HashSet<string> Reserved = ["SOS", "XXX", "PAN", "TTT", "VFR", "IFR", "VMC", "IMC"];

    public static string? ProhibitedReason(string marks)
    {
        if (marks.Length > 0 && marks[0] == 'Q')
            return "combinations starting with Q are not allowed";
        if (marks.Length > 1 && marks[1] == 'W')
            return "W cannot be the second letter";
        if (Reserved.Contains(marks))
            return $"{marks} is a reserved combination";

        return null;
    }

    public static bool IsAllowed(string marks) => ProhibitedReason(marks) is null;
}