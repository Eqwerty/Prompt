using System.Diagnostics;
using System.Reflection;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class ConfigCommand
{
    internal static void Run()
    {
        var configPath = GetConfigFilePath();
        EnsureConfigFileExists(configPath);

        var editor = GetEditor();

        try
        {
            var processStartInfo = new ProcessStartInfo(editor) { UseShellExecute = false };
            processStartInfo.ArgumentList.Add(configPath);

            Process.Start(processStartInfo)?.WaitForExit();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"gitprompt: failed to open editor '{editor}': {exception.Message}");
            Console.Error.WriteLine($"gitprompt: config file is at: {configPath}");

            Environment.Exit(1);
        }
    }

    internal static string GetConfigFilePath() => Path.Combine(XdgPaths.GetConfigDirectory(), "config.json");

    private static string GetEditor(string? editorEnv, string? visualEnv)
    {
        if (!string.IsNullOrEmpty(editorEnv))
        {
            return editorEnv;
        }

        if (!string.IsNullOrEmpty(visualEnv))
        {
            return visualEnv;
        }

        return "vim";
    }

    private static string GetEditor() => GetEditor(Environment.GetEnvironmentVariable("EDITOR"), Environment.GetEnvironmentVariable("VISUAL"));

    private static void EnsureConfigFileExists(string configPath)
    {
        if (File.Exists(configPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("default-config.json")!;
        using var fileStream = File.Create(configPath);
        stream.CopyTo(fileStream);
    }
}
