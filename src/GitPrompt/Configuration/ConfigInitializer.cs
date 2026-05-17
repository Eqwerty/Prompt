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
            .Replace("{gitStatusTtl}", JsonDouble(config.Cache?.GitStatusTtlSeconds, Config.CacheConfig.DefaultGitStatusTtlSeconds))
            .Replace("{repositoryTtl}", JsonDouble(config.Cache?.RepositoryTtlSeconds, Config.CacheConfig.DefaultRepositoryTtlSeconds))
            .Replace("{commandTimeoutMs}", JsonDouble(config.CommandTimeoutMs, Config.DefaultCommandTimeoutMs))
            .Replace("{commandDurationShow}", JsonBool(config.CommandDuration?.Show, Config.CommandDurationConfig.DefaultShow))
            .Replace("{commandDurationMinMs}", JsonNullableDouble(config.CommandDuration?.MinMs))
            .Replace("{showUser}", JsonBool(config.Context?.ShowUser, Config.ContextConfig.DefaultShowUser))
            .Replace("{showDomain}", JsonBool(config.Context?.ShowDomain, Config.ContextConfig.DefaultShowDomain))
            .Replace("{showHost}", JsonBool(config.Context?.ShowHost, Config.ContextConfig.DefaultShowHost))
            .Replace("{maxPathDepth}", JsonInt(config.Context?.MaxPathDepth, Config.ContextConfig.DefaultMaxPathDepth))
            .Replace("{multiline}", JsonBool(config.Layout?.Multiline, Config.LayoutConfig.DefaultMultiline))
            .Replace("{newlineBefore}", JsonBool(config.Layout?.NewlineBefore, Config.LayoutConfig.DefaultNewlineBefore))
            .Replace("{startOfLine}", JsonBool(config.Layout?.StartOfLine, Config.LayoutConfig.DefaultStartOfLine))
            .Replace("{symbol}", JsonValue(config.Layout?.Symbol))
            .Replace("{compact}", JsonBool(config.Compact, Config.DefaultCompact))
            .Replace("{showStash}", JsonBool(config.ShowStash, Config.DefaultShowStash))
            .Replace("{iconAhead}", JsonValue(config.Icons?.Ahead ?? PromptIcons.IconAhead.ToString()))
            .Replace("{iconBehind}", JsonValue(config.Icons?.Behind ?? PromptIcons.IconBehind.ToString()))
            .Replace("{iconAdded}", JsonValue(config.Icons?.Added ?? PromptIcons.IconAdded.ToString()))
            .Replace("{iconModified}", JsonValue(config.Icons?.Modified ?? PromptIcons.IconModified.ToString()))
            .Replace("{iconRenamed}", JsonValue(config.Icons?.Renamed ?? PromptIcons.IconRenamed.ToString()))
            .Replace("{iconDeleted}", JsonValue(config.Icons?.Deleted ?? PromptIcons.IconDeleted.ToString()))
            .Replace("{iconUntracked}", JsonValue(config.Icons?.Untracked ?? PromptIcons.IconUntracked.ToString()))
            .Replace("{iconConflicts}", JsonValue(config.Icons?.Conflicts ?? PromptIcons.IconConflicts.ToString()))
            .Replace("{iconStash}", JsonValue(config.Icons?.Stash ?? PromptIcons.IconStash.ToString()))
            .Replace("{iconDirty}", JsonValue(config.Icons?.Dirty ?? PromptIcons.IconDirty.ToString()))
            .Replace("{iconClean}", JsonValue(config.Icons?.Clean ?? PromptIcons.IconClean.ToString()))
            .Replace("{iconNoUpstreamMarker}", JsonValue(config.Icons?.NoUpstreamMarker ?? BranchLabelTokens.NoUpstreamBranchMarker))
            .Replace("{iconDetachedHeadMarker}", JsonValue(config.Icons?.DetachedHeadMarker ?? BranchLabelTokens.DetachedHeadBranchMarker))
            .Replace("{iconBranchLabelOpen}", JsonValue(config.Icons?.BranchLabelOpen ?? BranchLabelTokens.BranchLabelOpen))
            .Replace("{iconBranchLabelClose}", JsonValue(config.Icons?.BranchLabelClose ?? BranchLabelTokens.BranchLabelClose))
            .Replace("{iconBranchOperationSeparator}", JsonValue(config.Icons?.BranchOperationSeparator ?? BranchLabelTokens.BranchOperationSeparator))
            .Replace("{iconBranchLabelOpenNormal}", JsonValue(config.Icons?.BranchLabelOpenNormal ?? BranchLabelTokens.NormalBranchLabelOpen))
            .Replace("{iconBranchLabelCloseNormal}", JsonValue(config.Icons?.BranchLabelCloseNormal ?? BranchLabelTokens.NormalBranchLabelClose))
            .Replace("{iconBranchLabelOpenNoUpstream}", JsonValue(config.Icons?.BranchLabelOpenNoUpstream ?? BranchLabelTokens.NoUpstreamBranchLabelOpen))
            .Replace("{iconBranchLabelCloseNoUpstream}", JsonValue(config.Icons?.BranchLabelCloseNoUpstream ?? BranchLabelTokens.NoUpstreamBranchLabelClose))
            .Replace("{iconBranchLabelOpenDetached}", JsonValue(config.Icons?.BranchLabelOpenDetached ?? BranchLabelTokens.DetachedBranchLabelOpen))
            .Replace("{iconBranchLabelCloseDetached}", JsonValue(config.Icons?.BranchLabelCloseDetached ?? BranchLabelTokens.DetachedBranchLabelClose))
            .Replace("{colorUser}", JsonValue(config.Colors?.User ?? ColorDisplayValue(AnsiColors.Green)))
            .Replace("{colorHost}", JsonValue(config.Colors?.Host ?? ColorDisplayValue(AnsiColors.Magenta)))
            .Replace("{colorPath}", JsonValue(config.Colors?.Path ?? ColorDisplayValue(AnsiColors.Orange)))
            .Replace("{colorCommandDuration}", JsonValue(config.Colors?.CommandDuration ?? ColorDisplayValue(AnsiColors.Magenta)))
            .Replace("{colorBranch}", JsonValue(config.Colors?.Branch ?? ColorDisplayValue(AnsiColors.BoldCyan)))
            .Replace("{colorBranchNoUpstream}", JsonValue(config.Colors?.BranchNoUpstream ?? ColorDisplayValue(AnsiColors.BoldCyan)))
            .Replace("{colorBranchDetached}", JsonValue(config.Colors?.BranchDetached ?? ColorDisplayValue(AnsiColors.NormalYellow)))
            .Replace("{colorAhead}", JsonValue(config.Colors?.Ahead ?? ColorDisplayValue(AnsiColors.BoldCyan)))
            .Replace("{colorBehind}", JsonValue(config.Colors?.Behind ?? ColorDisplayValue(AnsiColors.BoldCyan)))
            .Replace("{colorStaged}", JsonValue(config.Colors?.Staged ?? ColorDisplayValue(AnsiColors.Green)))
            .Replace("{colorUnstaged}", JsonValue(config.Colors?.Unstaged ?? ColorDisplayValue(AnsiColors.Red)))
            .Replace("{colorUntracked}", JsonValue(config.Colors?.Untracked ?? ColorDisplayValue(AnsiColors.Red)))
            .Replace("{colorStash}", JsonValue(config.Colors?.Stash ?? ColorDisplayValue(AnsiColors.Magenta)))
            .Replace("{colorConflict}", JsonValue(config.Colors?.Conflict ?? ColorDisplayValue(AnsiColors.Red)))
            .Replace("{colorDirty}", JsonValue(config.Colors?.Dirty ?? ColorDisplayValue(AnsiColors.Orange)))
            .Replace("{colorClean}", JsonValue(config.Colors?.Clean ?? ColorDisplayValue(AnsiColors.Green)))
            .Replace("{colorMissingPath}", JsonValue(config.Colors?.MissingPath ?? ColorDisplayValue(AnsiColors.Red)))
            .Replace("{colorTimeout}", JsonValue(config.Colors?.Timeout ?? ColorDisplayValue(AnsiColors.Yellow)))
            .Replace("{colorPromptSymbol}", JsonValue(config.Colors?.PromptSymbol ?? ColorDisplayValue(AnsiColors.White)));
    }

    private static string JsonBool(bool? value, bool fallback)
    {
        return value ?? fallback ? "true" : "false";
    }

    private static string JsonInt(int? value, int fallback)
    {
        return (value ?? fallback).ToString(CultureInfo.InvariantCulture);
    }

    private static string JsonDouble(double? value, double fallback)
    {
        return (value ?? fallback).ToString(CultureInfo.InvariantCulture);
    }

    private static string JsonNullableDouble(double? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";
    }

    private static string JsonValue(string? value)
    {
        return value is null ? "null" : $"\"{value}\"";
    }

    private static string ColorDisplayValue(string ansiColor)
    {
        if (ansiColor.Length > 0 && ansiColor[0] is '\e')
        {
            return ansiColor[1..];
        }

        return ansiColor;
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

            Config userConfig;
            try
            {
                userConfig = JsonSerializer.Deserialize(fileContent, ConfigJsonContext.Default.Config) ?? new Config();
            }
            catch
            {
                return;
            }

            try
            {
                File.WriteAllText(configPath, BuildConfigContent(userConfig));
            }
            catch
            {
                // Non-critical: if writing fails, the old file is preserved as-is.
            }
        }
    }

    internal static Config MergeWithDefaults(Config userConfig)
    {
        return userConfig with
        {
            Compact = userConfig.Compact ?? Config.DefaultCompact,
            ShowStash = userConfig.ShowStash ?? Config.DefaultShowStash,
            CommandDuration = new Config.CommandDurationConfig
            {
                Show = userConfig.CommandDuration?.Show ?? Config.CommandDurationConfig.DefaultShow,
                MinMs = userConfig.CommandDuration?.MinMs
            },
            Context = new Config.ContextConfig
            {
                ShowUser = userConfig.Context?.ShowUser ?? Config.ContextConfig.DefaultShowUser,
                ShowDomain = userConfig.Context?.ShowDomain ?? Config.ContextConfig.DefaultShowDomain,
                ShowHost = userConfig.Context?.ShowHost ?? Config.ContextConfig.DefaultShowHost,
                MaxPathDepth = userConfig.Context?.MaxPathDepth ?? Config.ContextConfig.DefaultMaxPathDepth
            },
            Layout = new Config.LayoutConfig
            {
                Multiline = userConfig.Layout?.Multiline ?? Config.LayoutConfig.DefaultMultiline,
                NewlineBefore = userConfig.Layout?.NewlineBefore ?? Config.LayoutConfig.DefaultNewlineBefore,
                StartOfLine = userConfig.Layout?.StartOfLine ?? Config.LayoutConfig.DefaultStartOfLine,
                Symbol = userConfig.Layout?.Symbol
            },
            Cache = userConfig.Cache ?? new Config.CacheConfig(),
            Icons = userConfig.Icons ?? new Config.IconsConfig(),
            Colors = userConfig.Colors ?? new Config.ColorsConfig()
        };
    }

    private static bool HasMissingKeys(JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind is not JsonValueKind.Object || actual.ValueKind is not JsonValueKind.Object)
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
