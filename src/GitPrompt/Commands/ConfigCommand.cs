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

        var editor = EditorResolver.GetEditor();

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
}
