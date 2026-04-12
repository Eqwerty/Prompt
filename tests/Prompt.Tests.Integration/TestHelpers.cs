using System.Diagnostics;
using static Prompt.Constants.BranchLabelTokens;

namespace Prompt.Tests.Integration;

internal static class TestHelpers
{
    internal sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Integration", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            // Ensure current directory is not the one being deleted
            var currentDir = Directory.GetCurrentDirectory();
            if (currentDir.StartsWith(DirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.SetCurrentDirectory(Path.GetTempPath());
                }
                catch
                {
                    // Ignore errors setting directory
                }
            }

            if (Directory.Exists(DirectoryPath))
            {
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                    }

                    Directory.Delete(DirectoryPath, recursive: true);
                }
                catch (IOException)
                {
                    // Silently ignore cleanup errors - the OS will eventually clean up temp directories
                }
            }
        }
    }

    internal static async Task<string> ExecuteInDirectoryAsync(string directoryPath, Func<Task<string>> operation)
    {
        var previousDirectoryPath = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directoryPath);
            return await operation();
        }
        finally
        {
            try
            {
                Directory.SetCurrentDirectory(previousDirectoryPath);
            }
            catch (DirectoryNotFoundException)
            {
                // If the previous directory was deleted, set to a known valid directory
                Directory.SetCurrentDirectory(Path.GetTempPath());
            }
        }
    }

    internal static async Task ConfigureGitIdentityAsync(string repositoryPath)
    {
        await RunGitAsync(repositoryPath, "config user.name \"Prompt Integration Tests\"");
        await RunGitAsync(repositoryPath, "config user.email \"prompt-integration-tests@example.com\"");
    }

    internal static async Task<string> RunGitAsync(string workingDirectoryPath, string arguments)
    {
        var commandResult = await RunGitAllowFailureAsync(workingDirectoryPath, arguments);
        if (commandResult.ExitCode is not 0)
        {
            throw new InvalidOperationException($"git {arguments} failed in {workingDirectoryPath}: {commandResult.StandardError}");
        }

        return commandResult.StandardOutput;
    }

    internal static async Task<GitCommandResult> RunGitAllowFailureAsync(string workingDirectoryPath, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new GitCommandResult(process.ExitCode, standardOutput, standardError);
    }

    internal static string Quote(string value)
    {
        return "\"" + value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    internal static string TrackedBranchLabel(string branchName) => $"{BranchLabelOpen}{branchName}{BranchLabelClose}";

    internal static string NoUpstreamBranchLabel(string branchName) => $"{NoUpstreamBranchMarker}{TrackedBranchLabel(branchName)}";

    internal static string BranchLabelWithOperation(string branchLabel, string operation) =>
        branchLabel.Replace(BranchLabelClose, $"|{operation}{BranchLabelClose}", StringComparison.Ordinal);

    internal static string Indicator(char icon, int count) => $"{icon}{count}";

    internal readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
