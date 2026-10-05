using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Reporting.Services;

public sealed class ReportingServiceException : Exception
{
    public int StatusCode { get; }

    public ReportingServiceException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
