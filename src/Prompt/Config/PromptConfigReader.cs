using System.Text.Json;

namespace Prompt.Config;

internal static class PromptConfigReader
{
    private static readonly Lazy<PromptConfig> LazyConfig = new(ReadConfig);
    private static PromptConfig? _configOverride;

    internal static PromptConfig Config => _configOverride ?? LazyConfig.Value;

    internal static IDisposable OverrideForTesting(PromptConfig config)
    {
        var previous = _configOverride;
        _configOverride = config;

        return new ConfigOverride(() => _configOverride = previous);
    }

    private static PromptConfig ReadConfig()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (!File.Exists(configPath))
            {
                return new PromptConfig();
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize(json, PromptConfigJsonContext.Default.PromptConfig);

            return config is null
                ? new PromptConfig()
                : config with { Cache = config.Cache };
        }
        catch
        {
            return new PromptConfig();
        }
    }

    private sealed class ConfigOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose() => _restore();
    }
}
