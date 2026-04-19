using System.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class ConfigCommand
{
    private const string DefaultConfigJson =
        """
        {
          // Cache configuration
          "cache": {

            // How long to cache the git status segment, in seconds.
            // Set to 0 to disable git status caching.
            "gitStatusTtl": 5,

            // How long to cache the repository location, in seconds.
            // Set to 0 to disable repository location caching.
            "repositoryTtl": 60

          }
        }
        """;

    internal static void Run()
    {
        var configPath = GetConfigFilePath();
        EnsureConfigFileExists(configPath);

        var editor = GetEditor();
        try
        {
            var psi = new ProcessStartInfo(editor) { UseShellExecute = false };
            psi.ArgumentList.Add(configPath);
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"gitprompt: failed to open editor '{editor}': {ex.Message}");
            Console.Error.WriteLine($"gitprompt: config file is at: {configPath}");
            Environment.Exit(1);
        }
    }

    internal static string GetConfigFilePath()
        => Path.Combine(XdgPaths.GetConfigDirectory(), "config.json");

    internal static string GetEditor()
        => GetEditor(
            Environment.GetEnvironmentVariable("EDITOR"),
            Environment.GetEnvironmentVariable("VISUAL"));

    internal static string GetEditor(string? editorEnv, string? visualEnv)
    {
        if (!string.IsNullOrEmpty(editorEnv)) return editorEnv;
        if (!string.IsNullOrEmpty(visualEnv)) return visualEnv;
        return OperatingSystem.IsWindows() ? "notepad.exe" : "vi";
    }

    private static void EnsureConfigFileExists(string configPath)
    {
        if (File.Exists(configPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, DefaultConfigJson);
    }
}
