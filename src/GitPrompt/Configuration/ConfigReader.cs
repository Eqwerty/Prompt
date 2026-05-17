using System.Text.Json;
using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigReader
{
    private static readonly Lazy<ConfigLoadResult> LazyLoadResult = new(LoadConfig);
    private static Config? _configOverride;

    internal static Config Config => _configOverride ?? LazyLoadResult.Value.Config;

    internal static ConfigLoadResult LoadResult => LazyLoadResult.Value;

    internal static IDisposable OverrideForTesting(Config config)
    {
        var previous = _configOverride;
        _configOverride = ConfigInitializer.MergeWithDefaults(config);

        return new ConfigOverride(() => _configOverride = previous);
    }

    private static ConfigLoadResult LoadConfig()
    {
        var filePath = AppPaths.GetConfigFilePath();

        if (!File.Exists(filePath))
        {
            return new ConfigLoadResult(filePath, ConfigLoadStatus.Missing, ConfigInitializer.MergeWithDefaults(new Config()));
        }

        string json;
        try
        {
            json = File.ReadAllText(filePath);
        }
        catch
        {
            return new ConfigLoadResult(filePath, ConfigLoadStatus.ReadFailed, ConfigInitializer.MergeWithDefaults(new Config()));
        }

        try
        {
            var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);
            var resolved = ConfigInitializer.MergeWithDefaults(config ?? new Config());

            return new ConfigLoadResult(filePath, ConfigLoadStatus.Loaded, resolved);
        }
        catch
        {
            return new ConfigLoadResult(filePath, ConfigLoadStatus.ParseFailed, ConfigInitializer.MergeWithDefaults(new Config()));
        }
    }

    private sealed class ConfigOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose() => _restore();
    }
}
