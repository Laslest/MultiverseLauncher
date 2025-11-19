using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CrystalLauncher.Models;

public sealed class LauncherConfig
{
    [JsonPropertyName("rawConfigUrl")]
    public string? RawConfigUrl { get; set; }

    [JsonPropertyName("gameTitle")]
    public string? GameTitle { get; set; }

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("buildVersion")]
    public string? BuildVersion { get; set; }

    [JsonPropertyName("gameExecutable")]
    public string? GameExecutable { get; set; }

    [JsonPropertyName("clientDirectory")]
    public string? ClientDirectory { get; set; }

    [JsonPropertyName("assetsManifest")]
    public string? AssetsManifest { get; set; }

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("newsFeed")]
    public string? NewsFeed { get; set; }

    [JsonPropertyName("patchNotesFeed")]
    public string? PatchNotesFeed { get; set; }

    [JsonPropertyName("statusEndpoint")]
    public string? StatusEndpoint { get; set; }

    [JsonPropertyName("defaultServerState")]
    public string? DefaultServerState { get; set; }
        [JsonPropertyName("downloadPackageUrl")]
        public string? DownloadPackageUrl { get; set; }

        [JsonPropertyName("downloadPackageFileName")]
        public string? DownloadPackageFileName { get; set; }

    [JsonPropertyName("news")]
    public List<NewsEntry>? News { get; set; }

    [JsonPropertyName("patchNotes")]
    public List<PatchNoteEntry>? PatchNotes { get; set; }
}
