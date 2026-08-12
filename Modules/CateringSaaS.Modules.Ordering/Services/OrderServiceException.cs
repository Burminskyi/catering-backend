namespace CateringSaaS.Modules.Ordering.Services;

public sealed class OrderServiceException : Exception
{
    public int StatusCode { get; }

    public OrderServiceException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
