namespace CateringSaaS.Modules.Tenants.Domain;

/// <summary>
/// How far ahead this B2B office typically plans employee meal requests / consolidation.
/// Independent of menu publication and commercial contract terms.
/// </summary>
public enum ClientOrderCadence
{
    /// <summary>Next-day (or same-day) consolidation focus.</summary>
    Daily = 0,

    /// <summary>Employees and office manager plan roughly a week ahead.</summary>
    Weekly = 1
}
