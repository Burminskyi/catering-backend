namespace CateringSaaS.Shared.MultiTenancy;

public interface ICurrentUserContext
{
    Guid UserId { get; }

    bool IsAuthenticated { get; }

    Guid? ClientCompanyId { get; }

    string? Role { get; }
}
