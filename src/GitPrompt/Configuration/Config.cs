using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitPrompt.Configuration;

internal sealed record Config
{
    [JsonInclude]
    internal CacheConfig Cache { get; init; } = new();

    [JsonInclude]
    [JsonPropertyName("showUser")]
    internal bool ShowUser { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("showCommandDuration")]
    internal bool ShowCommandDuration { get; init; } = true;

    [JsonInclude]
    [JsonPropertyName("commandTimeoutMs")]
    internal double? CommandTimeoutMs { get; init; }

    [JsonIgnore]
    internal TimeSpan? CommandTimeout
    {
        get
        {
            var ms = CommandTimeoutMs ?? 2000.0;

            return ms > 0 && double.IsFinite(ms) ? TimeSpan.FromMilliseconds(ms) : null;
        }
    }

    internal sealed record CacheConfig
    {
        [JsonInclude]
        [JsonPropertyName("gitStatusTtl")]
        internal double? GitStatusTtlSeconds { get; init; }

        [JsonInclude]
        [JsonPropertyName("repositoryTtl")]
        internal double? RepositoryTtlSeconds { get; init; }

        [JsonIgnore]
        internal TimeSpan GitStatusTtl => TimeSpan.FromSeconds(GitStatusTtlSeconds ?? 5.0);

        [JsonIgnore]
        internal TimeSpan RepositoryTtl => TimeSpan.FromSeconds(RepositoryTtlSeconds ?? 60.0);
    }
}

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal partial class ConfigJsonContext : JsonSerializerContext;
