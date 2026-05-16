using System.Diagnostics;
using GitPrompt.Configuration;
using GitPrompt.Git;
using static GitPrompt.Constants.BranchLabelTokens;

namespace GitPrompt.Tests.Integration;

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

    internal static string DetachedBranchLabel(string branchLabel)
    {
        return $"{DetachedHeadBranchMarker}{DetachedBranchLabelOpen}{branchLabel}{DetachedBranchLabelClose}";
    }

    internal static string TrackedBranchLabel(string branchName)
    {
        return $"{NormalBranchLabelOpen}{branchName}{NormalBranchLabelClose}";
    }

    internal static string NoUpstreamBranchLabel(string branchName)
    {
        return $"{NoUpstreamBranchMarker}{NoUpstreamBranchLabelOpen}{branchName}{NoUpstreamBranchLabelClose}";
    }

    internal static string BranchLabelWithOperation(string branchLabel, string operation)
    {
        return branchLabel.Replace(BranchLabelClose, $"|{operation}{BranchLabelClose}", StringComparison.Ordinal);
    }

    internal static string Indicator(char icon, int count)
    {
        return $"{icon}{count}";
    }

    internal readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Injects a fake <c>git</c> executable that sleeps for 30 seconds. This guarantees that any
    /// timeout shorter than 30 s fires reliably, regardless of how fast the real git binary starts
    /// on the host machine.
    /// On Unix, a shell script named <c>git</c> is placed in a temp directory prepended to PATH.
    /// On Windows, a <see cref="Utilities.OverrideProcessStartInfoForTesting"/> seam replaces the
    /// git process with <c>ping.exe -n 31 127.0.0.1</c>, which sleeps ~30 seconds and is always
    /// available on Windows. The PATH manipulation is skipped on Windows because <c>CreateProcess</c>
    /// cannot execute <c>.bat</c>/<c>.cmd</c> scripts directly.
    /// </summary>
    internal sealed class FakeSlowGitOverride : IDisposable
    {
        private readonly string? _originalPath;
        private readonly string _tempDirectory;
        private readonly IDisposable? _processStartInfoOverride;

        public FakeSlowGitOverride()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Integration.FakeGit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            if (OperatingSystem.IsWindows())
            {
                _processStartInfoOverride = Utilities.OverrideProcessStartInfoForTesting(info => new ProcessStartInfo
                {
                    FileName = "ping.exe",
                    Arguments = "-n 31 127.0.0.1",
                    RedirectStandardOutput = info.RedirectStandardOutput,
                    RedirectStandardError = info.RedirectStandardError,
                    UseShellExecute = info.UseShellExecute,
                    CreateNoWindow = info.CreateNoWindow,
                    WorkingDirectory = info.WorkingDirectory
                });
            }
            else
            {
                _originalPath = Environment.GetEnvironmentVariable("PATH");
                var fakeGitPath = Path.Combine(_tempDirectory, "git");
                File.WriteAllText(fakeGitPath, "#!/bin/sh\nsleep 30\n");
                File.SetUnixFileMode(fakeGitPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                Environment.SetEnvironmentVariable("PATH", _tempDirectory + Path.PathSeparator + (_originalPath ?? string.Empty));
            }
        }

        public void Dispose()
        {
            _processStartInfoOverride?.Dispose();

            if (!OperatingSystem.IsWindows())
            {
                Environment.SetEnvironmentVariable("PATH", _originalPath);
            }

            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Silently ignore cleanup errors - the OS will eventually clean up temp directories
            }
        }
    }

    internal sealed class GitStatusCacheOverride : IDisposable
    {
        private readonly IDisposable _configOverride;
        private readonly IDisposable _cacheDirectoryOverride;

        public GitStatusCacheOverride(string cacheDirectoryPath, TimeSpan ttl = default)
        {
            var effectiveTtl = ttl == default ? TimeSpan.FromMinutes(1) : ttl;
            _configOverride = ConfigReader.OverrideForTesting(new Config
            {
                Cache = new Config.CacheConfig { GitStatusTtlSeconds = effectiveTtl.TotalSeconds }
            });

            _cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectoryPath);
        }

        public void Dispose()
        {
            _configOverride.Dispose();
            _cacheDirectoryOverride.Dispose();
        }
    }
}
