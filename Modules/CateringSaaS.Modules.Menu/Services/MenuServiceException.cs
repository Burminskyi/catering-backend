namespace CateringSaaS.Modules.Menu.Services;

public sealed class MenuServiceException : Exception
{
    public int StatusCode { get; }

    public MenuServiceException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
