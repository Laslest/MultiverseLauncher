using System.Text.Json.Serialization;

namespace CrystalLauncher.Models;

public sealed record NewsEntry(
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("subtitle")] string Subtitle,
	[property: JsonPropertyName("publishedAt")] string PublishedAt,
	[property: JsonPropertyName("tag")] string Tag);
