namespace CateringSaaS.Shared.SeedData;

/// <summary>
/// Well-known IDs for development seed data shared across modules.
/// </summary>
public static class DevelopmentSeedIds
{
    public static readonly Guid MockWorkspaceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>EPAM Systems — primary seeded B2B client (office portal users attach here).</summary>
    public static readonly Guid MockClientCompanyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid SoftServeClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    public static readonly Guid SigmaSoftwareClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    public static readonly Guid GlobalLogicClientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

    public static readonly Guid DemoSupplierId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
    public static readonly Guid DemoSupplierAltId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");

    public const string MockSubdomain = "romashka";
    public const string DemoMenuName = "Demo Weekly Menu";
}
