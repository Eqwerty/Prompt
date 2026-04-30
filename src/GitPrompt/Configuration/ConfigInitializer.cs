using System.Globalization;
using System.Text.Json;
using GitPrompt.Constants;
using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            var configPath = AppPaths.GetConfigFilePath();
            EnsureConfigFileExists(configPath);
            MigrateConfigIfNeeded(configPath);
        }
        catch
        {
            // Non-critical: the binary works fine with default settings even when the config file is absent.
        }
    }

    internal static void EnsureConfigFileExists(string configPath)
    {
        if (File.Exists(configPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        File.WriteAllText(configPath, BuildDefaultConfigContent());
    }

    internal static string BuildDefaultConfigContent() => BuildConfigContent(new Config());

    internal static string BuildConfigContent(Config config)
    {
        using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.jsonc")!;
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();

        return template
            .Replace("{gitStatusTtl}", config.Cache.GitStatusTtl.TotalSeconds.ToString(CultureInfo.InvariantCulture))
            .Replace("{repositoryTtl}", config.Cache.RepositoryTtl.TotalSeconds.ToString(CultureInfo.InvariantCulture))
            .Replace("{commandTimeoutMs}", ((long)(config.CommandTimeout?.TotalMilliseconds ?? 0)).ToString(CultureInfo.InvariantCulture))
            .Replace("{showCommandDuration}", config.ShowCommandDuration.ToString().ToLowerInvariant())
            .Replace("{showUser}", config.ShowUser.ToString().ToLowerInvariant())
            .Replace("{showHost}", config.ShowHost.ToString().ToLowerInvariant())
            .Replace("{maxPathDepth}", config.MaxPathDepth.ToString(CultureInfo.InvariantCulture))
            .Replace("{multilinePrompt}", config.MultilinePrompt.ToString().ToLowerInvariant())
            .Replace("{newlineBeforePrompt}", config.NewlineBeforePrompt.ToString().ToLowerInvariant())
            .Replace("{promptSymbol}", JsonValue(config.PromptSymbol))
            .Replace("{compact}", config.Compact.ToString().ToLowerInvariant())
            .Replace("{showStashInCompactMode}", config.ShowStashInCompactMode.ToString().ToLowerInvariant())
            .Replace("{iconAhead}", JsonValue(config.Icons.Ahead))
            .Replace("{iconAheadDefault}", PromptIcons.IconAhead.ToString())
            .Replace("{iconBehind}", JsonValue(config.Icons.Behind))
            .Replace("{iconBehindDefault}", PromptIcons.IconBehind.ToString())
            .Replace("{iconAdded}", JsonValue(config.Icons.Added))
            .Replace("{iconAddedDefault}", PromptIcons.IconAdded.ToString())
            .Replace("{iconModified}", JsonValue(config.Icons.Modified))
            .Replace("{iconModifiedDefault}", PromptIcons.IconModified.ToString())
            .Replace("{iconRenamed}", JsonValue(config.Icons.Renamed))
            .Replace("{iconRenamedDefault}", PromptIcons.IconRenamed.ToString())
            .Replace("{iconDeleted}", JsonValue(config.Icons.Deleted))
            .Replace("{iconDeletedDefault}", PromptIcons.IconDeleted.ToString())
            .Replace("{iconUntracked}", JsonValue(config.Icons.Untracked))
            .Replace("{iconUntrackedDefault}", PromptIcons.IconUntracked.ToString())
            .Replace("{iconConflicts}", JsonValue(config.Icons.Conflicts))
            .Replace("{iconConflictsDefault}", PromptIcons.IconConflicts.ToString())
            .Replace("{iconStash}", JsonValue(config.Icons.Stash))
            .Replace("{iconStashDefault}", PromptIcons.IconStash.ToString())
            .Replace("{iconDirty}", JsonValue(config.Icons.Dirty))
            .Replace("{iconDirtyDefault}", PromptIcons.IconDirty.ToString())
            .Replace("{iconClean}", JsonValue(config.Icons.Clean))
            .Replace("{iconCleanDefault}", PromptIcons.IconClean.ToString())
            .Replace("{iconNoUpstreamMarker}", JsonValue(config.Icons.NoUpstreamMarker))
            .Replace("{iconNoUpstreamMarkerDefault}", BranchLabelTokens.NoUpstreamBranchMarker)
            .Replace("{iconBranchLabelOpen}", JsonValue(config.Icons.BranchLabelOpen))
            .Replace("{iconBranchLabelOpenDefault}", BranchLabelTokens.BranchLabelOpen)
            .Replace("{iconBranchLabelClose}", JsonValue(config.Icons.BranchLabelClose))
            .Replace("{iconBranchLabelCloseDefault}", BranchLabelTokens.BranchLabelClose)
            .Replace("{iconBranchOperationSeparator}", JsonValue(config.Icons.BranchOperationSeparator))
            .Replace("{iconBranchOperationSeparatorDefault}", BranchLabelTokens.BranchOperationSeparator)
            .Replace("{colorUser}", JsonValue(config.Colors.User))
            .Replace("{colorUserDefault}", AnsiColors.Green)
            .Replace("{colorHost}", JsonValue(config.Colors.Host))
            .Replace("{colorHostDefault}", AnsiColors.Magenta)
            .Replace("{colorPath}", JsonValue(config.Colors.Path))
            .Replace("{colorPathDefault}", AnsiColors.Orange)
            .Replace("{colorCommandDuration}", JsonValue(config.Colors.CommandDuration))
            .Replace("{colorCommandDurationDefault}", AnsiColors.Magenta)
            .Replace("{colorBranch}", JsonValue(config.Colors.Branch))
            .Replace("{colorBranchDefault}", AnsiColors.Blue)
            .Replace("{colorBranchNoUpstream}", JsonValue(config.Colors.BranchNoUpstream))
            .Replace("{colorBranchNoUpstreamDefault}", AnsiColors.Blue)
            .Replace("{colorAhead}", JsonValue(config.Colors.Ahead))
            .Replace("{colorAheadDefault}", AnsiColors.Blue)
            .Replace("{colorBehind}", JsonValue(config.Colors.Behind))
            .Replace("{colorBehindDefault}", AnsiColors.Blue)
            .Replace("{colorStaged}", JsonValue(config.Colors.Staged))
            .Replace("{colorStagedDefault}", AnsiColors.Green)
            .Replace("{colorUnstaged}", JsonValue(config.Colors.Unstaged))
            .Replace("{colorUnstagedDefault}", AnsiColors.Red)
            .Replace("{colorUntracked}", JsonValue(config.Colors.Untracked))
            .Replace("{colorUntrackedDefault}", AnsiColors.Red)
            .Replace("{colorStash}", JsonValue(config.Colors.Stash))
            .Replace("{colorStashDefault}", AnsiColors.Magenta)
            .Replace("{colorConflict}", JsonValue(config.Colors.Conflict))
            .Replace("{colorConflictDefault}", AnsiColors.BoldRed)
            .Replace("{colorDirty}", JsonValue(config.Colors.Dirty))
            .Replace("{colorDirtyDefault}", AnsiColors.Orange)
            .Replace("{colorClean}", JsonValue(config.Colors.Clean))
            .Replace("{colorCleanDefault}", AnsiColors.Green)
            .Replace("{colorMissingPath}", JsonValue(config.Colors.MissingPath))
            .Replace("{colorMissingPathDefault}", AnsiColors.BoldRed)
            .Replace("{colorTimeout}", JsonValue(config.Colors.Timeout))
            .Replace("{colorTimeoutDefault}", AnsiColors.Yellow)
            .Replace("{colorPromptSymbol}", JsonValue(config.Colors.PromptSymbol))
            .Replace("{colorPromptSymbolDefault}", AnsiColors.LightGray);
    }

    private static string JsonValue(string? value)
    {
        return value is null ? "null" : $"\"{value}\"";
    }

    internal static void MigrateConfigIfNeeded(string configPath)
    {
        string fileContent;
        try
        {
            fileContent = File.ReadAllText(configPath);
        }
        catch
        {
            return;
        }

        var jsonOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        JsonDocument userDoc;
        try
        {
            userDoc = JsonDocument.Parse(fileContent, jsonOptions);
        }
        catch
        {
            return;
        }

        using (userDoc)
        {
            var defaultContent = BuildDefaultConfigContent();
            using var defaultDoc = JsonDocument.Parse(defaultContent, jsonOptions);

            if (!HasMissingKeys(defaultDoc.RootElement, userDoc.RootElement))
            {
                return;
            }
        }

        Config userConfig;
        try
        {
            userConfig = JsonSerializer.Deserialize(fileContent, ConfigJsonContext.Default.Config) ?? new Config();
            userConfig = userConfig with
            {
                Cache = userConfig.Cache,
                Icons = userConfig.Icons,
                Colors = userConfig.Colors
            };
        }
        catch
        {
            return;
        }

        File.WriteAllText(configPath, BuildConfigContent(userConfig));
    }

    private static bool HasMissingKeys(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind != JsonValueKind.Object || actual.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var expectedProp in expected.EnumerateObject())
        {
            if (!actual.TryGetProperty(expectedProp.Name, out var actualValue))
            {
                return true;
            }

            if (HasMissingKeys(expectedProp.Value, actualValue))
            {
                return true;
            }
        }

        return false;
    }
}
