using System.Diagnostics; // Para Activity
using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Domain.Exceptions; // Importar

namespace StreamForge.API.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exceção não tratada: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier }
        };

        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Validation Failed";
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Detail = "One or more validation errors occurred.";
            
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            
            problemDetails.Extensions["errors"] = errors;
        }
        else if (exception is NotFoundException notFoundEx)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            problemDetails.Title = "Not Found";
            problemDetails.Status = (int)HttpStatusCode.NotFound;
            problemDetails.Detail = notFoundEx.Message;
        }
        else if (exception is DomainException domainEx)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Domain Rule Violation";
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Detail = domainEx.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            problemDetails.Title = "Unauthorized";
            problemDetails.Status = (int)HttpStatusCode.Unauthorized;
            problemDetails.Detail = exception.Message;
        }
        else if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Requisição cancelada pelo cliente.");
            
            httpContext.Response.StatusCode = 499; // Client Closed Request
            problemDetails.Title = "Request Cancelled";
            problemDetails.Status = 499;
            problemDetails.Detail = "The request was cancelled by the client.";
        }
        else
        {
            _logger.LogError(exception, "Exceção não tratada: {Message}", exception.Message);
            
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            problemDetails.Title = "Internal Server Error";
            problemDetails.Status = (int)HttpStatusCode.InternalServerError;
            problemDetails.Detail = "An unexpected error occurred.";
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
