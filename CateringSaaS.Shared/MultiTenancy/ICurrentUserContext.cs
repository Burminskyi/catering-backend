namespace CateringSaaS.Shared.MultiTenancy;

public interface ICurrentUserContext
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }
}
