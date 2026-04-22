using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            var configFile = AppPaths.GetConfigFilePath();

            if (File.Exists(configFile))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(configFile)!);

            using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.jsonc")!;

            using var fileStream = File.Create(configFile);
            stream.CopyTo(fileStream);
        }
        catch
        {
            // Non-critical: the binary works fine with default settings even when the config file is absent.
        }
    }
}
