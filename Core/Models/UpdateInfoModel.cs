using System.Text.Json.Serialization;

namespace Core.Models
{
    /// <summary>
    /// Represents information about an update.
    /// </summary>
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        /// <summary>
        /// One bullet point per entry - mirrors the "### Changelog" bullet-list format used on the GitHub
        /// release page itself, so the same list can be rendered in either place.
        /// </summary>
        [JsonPropertyName("changelog")]
        public List<string>? Changelog { get; set; }
        /// <summary>
        /// SHA256 hash (lowercase hex) of the self-contained release zip for <see cref="Version"/>, published by the
        /// release workflow. Verified by the updater before extracting a downloaded update, as defense-in-depth
        /// beyond plain HTTPS/TLS.
        /// </summary>
        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }
        [JsonPropertyName("historic_versions")]
        public List<HistoricVersion>? HistoricVersions { get; set; }

        /// <summary>
        /// Represents historical version information.
        /// </summary>
        public class HistoricVersion
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("changelog")]
            public List<string>? Changelog { get; set; }
        }
    }
}
