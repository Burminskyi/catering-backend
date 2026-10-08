namespace CateringSaaS.Modules.Knowledge.Configuration;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>S3-compatible endpoint (e.g. https://&lt;accountid&gt;.r2.cloudflarestorage.com).</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;
}
