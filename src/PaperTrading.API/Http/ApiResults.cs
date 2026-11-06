using Microsoft.AspNetCore.Mvc;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.API.Http;

public static class ApiResults
{
    public static IActionResult From<T>(Result<T> result, HttpContext httpContext, Func<T, IActionResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error, httpContext);

    public static IActionResult Problem(Error error, HttpContext httpContext)
    {
        if (error is ValidationError validation)
        {
            var details = new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            };

            return new ObjectResult(details)
            {
                StatusCode = details.Status,
                ContentTypes = { "application/problem+json" }
            };
        }

        var status = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden when error.Code == "Auth.LockedOut" => StatusCodes.Status423Locked,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = error.Description,
            Type = TypeFor(status),
            Instance = httpContext.Request.Path
        };

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status423Locked => "Locked",
        StatusCodes.Status403Forbidden => "Forbidden",
        _ => "Bad Request"
    };

    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status409Conflict => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
        StatusCodes.Status401Unauthorized => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
        StatusCodes.Status404NotFound => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
        StatusCodes.Status423Locked => "https://datatracker.ietf.org/doc/html/rfc4918#section-11.3",
        StatusCodes.Status403Forbidden => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
        _ => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"
    };
}
