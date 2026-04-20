using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            InitializeDefaultConfig(XdgPaths.GetConfigDirectory());
        }
        catch
        {
            // Non-critical: the binary works fine with default settings even when the config file is absent.
        }
    }

    // Exposed for testing.
    internal static void InitializeDefaultConfig(string configDir)
    {
        var configFile = Path.Combine(configDir, "config.json");

        if (File.Exists(configFile))
        {
            return;
        }

        Directory.CreateDirectory(configDir);

        using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.json")!;

        using var fileStream = File.Create(configFile);
        stream.CopyTo(fileStream);
    }
}
