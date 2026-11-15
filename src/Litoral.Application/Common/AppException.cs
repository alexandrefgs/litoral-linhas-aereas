namespace Litoral.Application.Common;

public enum AppErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict
}

public sealed class AppException(AppErrorKind kind, string message) : Exception(message)
{
    public AppErrorKind Kind { get; } = kind;
}