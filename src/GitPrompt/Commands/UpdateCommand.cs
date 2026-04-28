using System.Diagnostics;
using GitPrompt.Platform;
using GitPrompt.Terminal;

namespace GitPrompt.Commands;

internal static class UpdateCommand
{
    private const string InstallScriptUrl = "https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh";
    private const string AliasesUrl = "https://github.com/Eqwerty/GitPrompt/releases/download/latest/git_aliases.sh";
    private const string AliasesFileName = "git_aliases.sh";
    private const string GitCompletionUrl = "https://raw.githubusercontent.com/git/git/master/contrib/completion/git-completion.bash";
    private const string GitCompletionFileName = "git-completion.bash";

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

    internal static void RunUpdateAliases()
    {
        var dataDir = XdgPaths.GetDataDirectory();
        var aliasesPath = Path.Combine(dataDir, AliasesFileName);
        var completionPath = Path.Combine(dataDir, GitCompletionFileName);
        string[] curlSslArgs = OperatingSystem.IsWindows() ? ["--ssl-no-revoke"] : [];

        Directory.CreateDirectory(dataDir);

        var cancelled = false;
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cancelled = true;
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            bool aliasesOk;
            using (var spinner = TerminalSpinner.Start("Downloading git aliases"))
            {
                aliasesOk = DownloadFile(AliasesUrl, aliasesPath, curlSslArgs);
                if (aliasesOk)
                {
                    spinner.Complete();
                }
            }

            if (cancelled)
            {
                Console.Error.Write($"\n{AnsiTerminal.Red}error:{AnsiTerminal.Reset} Cancelled.");

                Environment.Exit(130);
            }

            if (!aliasesOk)
            {
                Console.Error.WriteLine("gitprompt: update aliases failed.");
                Console.Error.WriteLine($"gitprompt: to update manually, run: curl -fsSL {AliasesUrl} -o \"{aliasesPath}\"");

                Environment.Exit(1);
            }

            if (File.Exists(completionPath))
            {
                bool completionOk;
                using (var spinner = TerminalSpinner.Start("Updating git completions"))
                {
                    completionOk = DownloadFile(GitCompletionUrl, completionPath, curlSslArgs);

                    if (completionOk)
                    {
                        spinner.Complete();
                    }
                }

                if (cancelled)
                {
                    Console.Error.Write($"\n{AnsiTerminal.Red}error:{AnsiTerminal.Reset} Cancelled.");

                    Environment.Exit(130);
                }

                if (!completionOk)
                {
                    Console.Error.WriteLine(
                        $"{AnsiTerminal.Yellow}warning:{AnsiTerminal.Reset} Git completions update failed. Alias tab completion may not work.");
                }
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"gitprompt: update aliases failed: {exception.Message}");
            Console.Error.WriteLine($"gitprompt: to update manually, run: curl -fsSL {AliasesUrl} -o \"{aliasesPath}\"");

            Environment.Exit(1);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        Console.Write("\nRestart your terminal to apply changes.");
    }

    private static bool DownloadFile(string url, string outputPath, string[] curlSslArgs)
    {
        var psi = new ProcessStartInfo("curl") { UseShellExecute = false };
        psi.ArgumentList.Add("-fsSL");
        foreach (var arg in curlSslArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        psi.ArgumentList.Add(url);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outputPath);

        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start curl process.");
        process.WaitForExit();

        return process.ExitCode is 0;
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
