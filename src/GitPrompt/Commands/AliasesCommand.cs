using System.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class AliasesCommand
{
    internal static void Run(string? aliasesPath = null, TextWriter? errorOutput = null)
    {
        aliasesPath ??= AppPaths.GetAliasesFilePath();
        errorOutput ??= Console.Error;

        if (!File.Exists(aliasesPath))
        {
            errorOutput.WriteLine($"gitprompt: git aliases not found at: {aliasesPath}");
            errorOutput.WriteLine("gitprompt: run 'gitprompt update aliases' to install them");
            
            return;
        }

        var editor = EditorResolver.GetEditor();

        try
        {
            var processStartInfo = new ProcessStartInfo(editor) { UseShellExecute = false };
            processStartInfo.ArgumentList.Add(aliasesPath);

            Process.Start(processStartInfo)?.WaitForExit();
        }
        catch (Exception exception)
        {
            errorOutput.WriteLine($"gitprompt: failed to open editor '{editor}': {exception.Message}");
            errorOutput.WriteLine($"gitprompt: aliases file is at: {aliasesPath}");

            Environment.Exit(1);
        }
    }
}
