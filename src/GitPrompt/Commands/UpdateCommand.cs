using System.Diagnostics;

namespace GitPrompt.Commands;

internal static class UpdateCommand
{
    private const string InstallScriptUrl = "https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh";

    internal static void Run()
    {
        CleanUpOldBinary();

        var sslOption = OperatingSystem.IsWindows() ? "--ssl-no-revoke " : "";
        var script = $"curl -fsSL {sslOption}{InstallScriptUrl} | sh";

        try
        {
            var processStartInfo = new ProcessStartInfo("sh")
            {
                UseShellExecute = false
            };

            processStartInfo.ArgumentList.Add("-c");
            processStartInfo.ArgumentList.Add(script);

            var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start shell process.");

            process.WaitForExit();

            if (process.ExitCode is not 0)
            {
                Environment.Exit(process.ExitCode);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"gitprompt: update failed: {exception.Message}");
            Console.Error.WriteLine($"gitprompt: to update manually, run: curl -fsSL {InstallScriptUrl} | sh");

            Environment.Exit(1);
        }
    }

    private static void CleanUpOldBinary()
    {
        try
        {
            var currentPath = Environment.ProcessPath;
            if (currentPath is null)
            {
                return;
            }

            var oldPath = currentPath + ".old";
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }
        catch
        {
            // Best-effort; don't block the update if cleanup fails.
        }
    }
}
