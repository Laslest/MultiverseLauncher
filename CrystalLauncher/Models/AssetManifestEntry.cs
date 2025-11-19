using System.Text.Json.Serialization;

namespace CrystalLauncher.Models;

public sealed class AssetManifestEntry
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("localfile")]
    public string? LocalFile { get; set; }

    [JsonPropertyName("packedhash")]
    public string? PackedHash { get; set; }

    [JsonPropertyName("unpackedhash")]
    public string? UnpackedHash { get; set; }

    [JsonPropertyName("packedsize")]
    public long? PackedSize { get; set; }

    [JsonPropertyName("unpackedsize")]
    public long? UnpackedSize { get; set; }
}
