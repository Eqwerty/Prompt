using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prompt.Config;

internal sealed record PromptConfig
{
    internal CacheConfig Cache { get; init; } = new();

    internal sealed record CacheConfig
    {
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        internal TimeSpan? GitStatusTtl { get; init; }

        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        internal TimeSpan? RepositoryTtl { get; init; }
    }
}

/// <summary>Reads and writes <see cref="TimeSpan"/> as a number of seconds in JSON.</summary>
internal sealed class TimeSpanSecondsConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromSeconds(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.TotalSeconds);
}

[JsonSerializable(typeof(PromptConfig))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
internal partial class PromptConfigJsonContext : JsonSerializerContext;
