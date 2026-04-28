using System.Globalization;
using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            EnsureConfigFileExists(AppPaths.GetConfigFilePath());
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

    internal static string BuildDefaultConfigContent()
    {
        using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.jsonc")!;
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();

        var config = new Config();

        return template
            .Replace("{gitStatusTtl}", config.Cache.GitStatusTtl.TotalSeconds.ToString(CultureInfo.InvariantCulture))
            .Replace("{repositoryTtl}", config.Cache.RepositoryTtl.TotalSeconds.ToString(CultureInfo.InvariantCulture))
            .Replace("{commandTimeoutMs}", ((long)(config.CommandTimeout?.TotalMilliseconds ?? 0)).ToString(CultureInfo.InvariantCulture))
            .Replace("{showCommandDuration}", config.ShowCommandDuration.ToString().ToLowerInvariant())
            .Replace("{showUser}", config.ShowUser.ToString().ToLowerInvariant())
            .Replace("{showHost}", config.ShowHost.ToString().ToLowerInvariant())
            .Replace("{maxPathDepth}", config.MaxPathDepth.ToString(CultureInfo.InvariantCulture));
    }
}
