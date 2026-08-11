namespace CateringSaaS.Modules.Identity.Services;

public sealed class IdentityServiceException : Exception
{
    public int StatusCode { get; }

    public IdentityServiceException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
