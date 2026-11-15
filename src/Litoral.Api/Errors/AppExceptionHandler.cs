using Litoral.Application.Common;
using Litoral.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Litoral.Api.Errors;

public sealed class AppExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            AppException { Kind: AppErrorKind.Validation } => StatusCodes.Status400BadRequest,
            AppException { Kind: AppErrorKind.Unauthorized } => StatusCodes.Status401Unauthorized,
            AppException { Kind: AppErrorKind.NotFound } => StatusCodes.Status404NotFound,
            AppException { Kind: AppErrorKind.Conflict } => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status400BadRequest,
            _ => 0
        };

        if (status == 0)
            return false;

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = exception.Message }
        });
    }
}