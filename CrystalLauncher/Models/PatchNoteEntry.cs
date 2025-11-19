using System.Text.Json.Serialization;

namespace CrystalLauncher.Models;

public sealed record PatchNoteEntry(
	[property: JsonPropertyName("version")] string Version,
	[property: JsonPropertyName("summary")] string Summary);
