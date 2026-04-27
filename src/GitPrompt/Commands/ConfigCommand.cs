using System.Diagnostics;
using GitPrompt.Configuration;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class ConfigCommand
{
    internal static void Run()
    {
        var configPath = AppPaths.GetConfigFilePath();
        ConfigInitializer.EnsureConfigFileExists(configPath);

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
}
