using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitPrompt.Configuration;

internal sealed record Config
{
    [JsonInclude]
    internal CacheConfig Cache { get; init; } = new();

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
