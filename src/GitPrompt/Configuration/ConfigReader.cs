using System.Text.Json;
using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigReader
{
    private static readonly Lazy<Config> LazyConfig = new(ReadConfig);
    private static Config? _configOverride;

    internal static Config Config => _configOverride ?? LazyConfig.Value;

    internal static IDisposable OverrideForTesting(Config config)
    {
        var previous = _configOverride;
        _configOverride = config;

        return new ConfigOverride(() => _configOverride = previous);
    }

    private static Config ReadConfig()
    {
        try
        {
            var configurationPath = AppPaths.GetConfigFilePath();
            if (!File.Exists(configurationPath))
            {
                return new Config();
            }

            var json = File.ReadAllText(configurationPath);
            var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

            return config is null
                ? new Config()
                : config with { Cache = config.Cache };
        }
        catch
        {
            return new Config();
        }
    }

    private sealed class ConfigOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose() => _restore();
    }
}
