using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitPrompt.Configuration;

internal sealed record Config
{
    internal const bool DefaultCompact = false;
    internal const bool DefaultShowStash = true;
    internal const double DefaultCommandTimeoutMs = 2000;

    [JsonInclude]
    internal CacheConfig? Cache { get; init; }

    [JsonInclude]
    [JsonPropertyName("commandTimeoutMs")]
    internal double? CommandTimeoutMs { get; init; }

    [JsonInclude]
    [JsonPropertyName("commandDuration")]
    internal CommandDurationConfig? CommandDuration { get; init; }

    [JsonInclude]
    [JsonPropertyName("context")]
    internal ContextConfig? Context { get; init; }

    [JsonInclude]
    [JsonPropertyName("layout")]
    internal LayoutConfig? Layout { get; init; }

    [JsonInclude]
    [JsonPropertyName("compact")]
    internal bool? Compact { get; init; }

    [JsonInclude]
    [JsonPropertyName("showStash")]
    internal bool? ShowStash { get; init; }

    [JsonInclude]
    internal IconsConfig? Icons { get; init; }

    [JsonInclude]
    internal ColorsConfig? Colors { get; init; }

    [JsonIgnore]
    internal TimeSpan? CommandTimeout
    {
        get
        {
            var ms = CommandTimeoutMs ?? DefaultCommandTimeoutMs;

            return ms > 0 && double.IsFinite(ms) ? TimeSpan.FromMilliseconds(ms) : null;
        }
    }

    internal sealed record CacheConfig
    {
        internal const double DefaultGitStatusTtlSeconds = 5;
        internal const double DefaultRepositoryTtlSeconds = 60;

        [JsonInclude]
        [JsonPropertyName("gitStatusTtl")]
        internal double? GitStatusTtlSeconds { get; init; }

        [JsonInclude]
        [JsonPropertyName("repositoryTtl")]
        internal double? RepositoryTtlSeconds { get; init; }

        [JsonIgnore]
        internal TimeSpan GitStatusTtl => TimeSpan.FromSeconds(GitStatusTtlSeconds ?? DefaultGitStatusTtlSeconds);

        [JsonIgnore]
        internal TimeSpan RepositoryTtl => TimeSpan.FromSeconds(RepositoryTtlSeconds ?? DefaultRepositoryTtlSeconds);
    }

    internal sealed record CommandDurationConfig
    {
        internal const bool DefaultShow = true;

        [JsonInclude]
        [JsonPropertyName("show")]
        internal bool? Show { get; init; }

        [JsonInclude]
        [JsonPropertyName("minMs")]
        internal double? MinMs { get; init; }
    }

    internal sealed record ContextConfig
    {
        internal const bool DefaultShowUser = true;
        internal const bool DefaultShowDomain = false;
        internal const bool DefaultShowHost = true;
        internal const int DefaultMaxPathDepth = 0;

        [JsonInclude]
        [JsonPropertyName("showUser")]
        internal bool? ShowUser { get; init; }

        [JsonInclude]
        [JsonPropertyName("showDomain")]
        internal bool? ShowDomain { get; init; }

        [JsonInclude]
        [JsonPropertyName("showHost")]
        internal bool? ShowHost { get; init; }

        [JsonInclude]
        [JsonPropertyName("maxPathDepth")]
        internal int? MaxPathDepth { get; init; }
    }

    internal sealed record LayoutConfig
    {
        internal const bool DefaultMultiline = true;
        internal const bool DefaultNewlineBefore = false;
        internal const bool DefaultStartOfLine = true;

        [JsonInclude]
        [JsonPropertyName("multiline")]
        internal bool? Multiline { get; init; }

        [JsonInclude]
        [JsonPropertyName("newlineBefore")]
        internal bool? NewlineBefore { get; init; }

        [JsonInclude]
        [JsonPropertyName("startOfLine")]
        internal bool? StartOfLine { get; init; }

        [JsonInclude]
        [JsonPropertyName("symbol")]
        internal string? Symbol { get; init; }
    }

    internal sealed record IconsConfig
    {
        [JsonInclude]
        [JsonPropertyName("ahead")]
        internal string? Ahead { get; init; }

        [JsonInclude]
        [JsonPropertyName("behind")]
        internal string? Behind { get; init; }

        [JsonInclude]
        [JsonPropertyName("added")]
        internal string? Added { get; init; }

        [JsonInclude]
        [JsonPropertyName("modified")]
        internal string? Modified { get; init; }

        [JsonInclude]
        [JsonPropertyName("renamed")]
        internal string? Renamed { get; init; }

        [JsonInclude]
        [JsonPropertyName("deleted")]
        internal string? Deleted { get; init; }

        [JsonInclude]
        [JsonPropertyName("untracked")]
        internal string? Untracked { get; init; }

        [JsonInclude]
        [JsonPropertyName("conflicts")]
        internal string? Conflicts { get; init; }

        [JsonInclude]
        [JsonPropertyName("stash")]
        internal string? Stash { get; init; }

        [JsonInclude]
        [JsonPropertyName("dirty")]
        internal string? Dirty { get; init; }

        [JsonInclude]
        [JsonPropertyName("clean")]
        internal string? Clean { get; init; }

        [JsonInclude]
        [JsonPropertyName("noUpstreamMarker")]
        internal string? NoUpstreamMarker { get; init; }

        [JsonInclude]
        [JsonPropertyName("detachedHeadMarker")]
        internal string? DetachedHeadMarker { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelOpen")]
        internal string? BranchLabelOpen { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelClose")]
        internal string? BranchLabelClose { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelOpenNormal")]
        internal string? BranchLabelOpenNormal { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelCloseNormal")]
        internal string? BranchLabelCloseNormal { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelOpenNoUpstream")]
        internal string? BranchLabelOpenNoUpstream { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelCloseNoUpstream")]
        internal string? BranchLabelCloseNoUpstream { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelOpenDetached")]
        internal string? BranchLabelOpenDetached { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchLabelCloseDetached")]
        internal string? BranchLabelCloseDetached { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchOperationSeparator")]
        internal string? BranchOperationSeparator { get; init; }
    }

    internal sealed record ColorsConfig
    {
        [JsonInclude]
        [JsonPropertyName("user")]
        internal string? User { get; init; }

        [JsonInclude]
        [JsonPropertyName("host")]
        internal string? Host { get; init; }

        [JsonInclude]
        [JsonPropertyName("path")]
        internal string? Path { get; init; }

        [JsonInclude]
        [JsonPropertyName("commandDuration")]
        internal string? CommandDuration { get; init; }

        [JsonInclude]
        [JsonPropertyName("branch")]
        internal string? Branch { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchNoUpstream")]
        internal string? BranchNoUpstream { get; init; }

        [JsonInclude]
        [JsonPropertyName("branchDetached")]
        internal string? BranchDetached { get; init; }

        [JsonInclude]
        [JsonPropertyName("ahead")]
        internal string? Ahead { get; init; }

        [JsonInclude]
        [JsonPropertyName("behind")]
        internal string? Behind { get; init; }

        [JsonInclude]
        [JsonPropertyName("staged")]
        internal string? Staged { get; init; }

        [JsonInclude]
        [JsonPropertyName("unstaged")]
        internal string? Unstaged { get; init; }

        [JsonInclude]
        [JsonPropertyName("untracked")]
        internal string? Untracked { get; init; }

        [JsonInclude]
        [JsonPropertyName("stash")]
        internal string? Stash { get; init; }

        [JsonInclude]
        [JsonPropertyName("conflict")]
        internal string? Conflict { get; init; }

        [JsonInclude]
        [JsonPropertyName("dirty")]
        internal string? Dirty { get; init; }

        [JsonInclude]
        [JsonPropertyName("clean")]
        internal string? Clean { get; init; }

        [JsonInclude]
        [JsonPropertyName("missingPath")]
        internal string? MissingPath { get; init; }

        [JsonInclude]
        [JsonPropertyName("timeout")]
        internal string? Timeout { get; init; }

        [JsonInclude]
        [JsonPropertyName("promptSymbol")]
        internal string? PromptSymbol { get; init; }
    }
}

[JsonSerializable(typeof(Config))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal partial class ConfigJsonContext : JsonSerializerContext;
