namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>Bunny.net private storage. Keys stay on the API. S3 compatibility stays off.</summary>
public sealed class BunnyOptions
{
    public const string SectionName = "Bunny";

    public bool Enabled { get; set; }

    public string StorageZone { get; set; } = "";

    public string StorageEndpoint { get; set; } = "";

    public string AccessKey { get; set; } = "";
}
