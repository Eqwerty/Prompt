using System.Diagnostics;
using FluentAssertions;

namespace Prompt.Tests.Integration;

[Collection("GitStatusSerialTests")]
public sealed class GitStatusIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenTrackedBranchHasLocalAndRemoteCommits_ShouldShowBranchAndAheadBehindCounts()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {Quote(remoteRepositoryPath)}");
        await RunGitAsync(sandbox.DirectoryPath, $"clone {Quote(remoteRepositoryPath)} {Quote(sourceRepositoryPath)}");
        await ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await RunGitAsync(sourceRepositoryPath, "add base.txt");
        await RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        await RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await RunGitAsync(sandbox.DirectoryPath, $"clone {Quote(remoteRepositoryPath)} {Quote(localRepositoryPath)}");
        await ConfigureGitIdentityAsync(localRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "local-ahead.txt"), "ahead\n");
        await RunGitAsync(localRepositoryPath, "add local-ahead.txt");
        await RunGitAsync(localRepositoryPath, "commit -m \"local ahead\"");

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "remote-ahead.txt"), "behind\n");
        await RunGitAsync(sourceRepositoryPath, "add remote-ahead.txt");
        await RunGitAsync(sourceRepositoryPath, "commit -m \"remote ahead\"");
        await RunGitAsync(sourceRepositoryPath, "push");
        await RunGitAsync(localRepositoryPath, "fetch origin");

        // Act
        var gitStatusSegment = await ExecuteInDirectoryAsync(localRepositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        gitStatusSegment.Should().Contain("(main)");
        gitStatusSegment.Should().Contain("↑1");
        gitStatusSegment.Should().Contain("↓1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenBranchHasNoUpstreamAndLocalCommits_ShouldShowNoUpstreamMarkerAndAheadCount()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add base.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");
        await RunGitAsync(repositoryPath, "checkout -b feature");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "feature.txt"), "feature\n");
        await RunGitAsync(repositoryPath, "add feature.txt");
        await RunGitAsync(repositoryPath, "commit -m \"feature commit\"");

        // Act
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        gitStatusSegment.Should().Contain("*(feature)");
        gitStatusSegment.Should().Contain("↑1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenHeadIsDetached_ShouldShowCheckedOutCommit()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-a.txt"), "a\n");
        await RunGitAsync(repositoryPath, "add commit-a.txt");
        await RunGitAsync(repositoryPath, "commit -m \"commit a\"");
        var commitAObjectId = (await RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-b.txt"), "b\n");
        await RunGitAsync(repositoryPath, "add commit-b.txt");
        await RunGitAsync(repositoryPath, "commit -m \"commit b\"");

        await RunGitAsync(repositoryPath, $"checkout --detach {commitAObjectId}");

        // Act
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        gitStatusSegment.Should().Contain($"({commitAObjectId[..7]}...)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenStashExists_ShouldShowStashMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add tracked.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "changed\n");
        await RunGitAsync(repositoryPath, "stash push -m \"wip\"");

        // Act
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        gitStatusSegment.Should().Contain("@1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenMergeIsInProgress_ShouldShowMergeOperationMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add conflict.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act
        var mergeCommandResult = await RunGitAllowFailureAsync(repositoryPath, "merge feature");
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|MERGE");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenCherryPickIsInProgress_ShouldShowCherryPickOperationMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add conflict.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        await RunGitAsync(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = (await RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act
        var cherryPickCommandResult = await RunGitAllowFailureAsync(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|CHERRY-PICK");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsMerging_ShouldShowMergeOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add conflict.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await RunGitAsync(repositoryPath, "checkout -b other main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "other\n");
        await RunGitAsync(repositoryPath, "commit -am \"other change\"");

        await RunGitAsync(repositoryPath, "checkout feature");

        // Act
        var mergeCommandResult = await RunGitAllowFailureAsync(repositoryPath, "merge other");
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("*(feature|MERGE)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsCherryPicking_ShouldShowCherryPickOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add conflict.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        await RunGitAsync(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = (await RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await RunGitAsync(repositoryPath, "checkout -b feature main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        // Act
        var cherryPickCommandResult = await RunGitAllowFailureAsync(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("*(feature|CHERRY-PICK)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRebaseIsInProgress_ShouldShowBranchNameInsteadOfDetachedCommit()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add conflict.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await RunGitAsync(repositoryPath, "commit -am \"main change\"");

        await RunGitAsync(repositoryPath, "checkout feature");

        // Act
        var rebaseCommandResult = await RunGitAllowFailureAsync(repositoryPath, "rebase main");
        var gitStatusSegment = await ExecuteInDirectoryAsync(repositoryPath, GitStatusSegmentBuilder.BuildAsync);

        // Assert
        rebaseCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("feature|REBASE");
        gitStatusSegment.Should().NotContain("...|REBASE");
    }

    private sealed class TemporaryDirectory : IDisposable
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
                foreach (var filePath in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }

    private static async Task<string> ExecuteInDirectoryAsync(string directoryPath, Func<Task<string>> operation)
    {
        var previousDirectoryPath = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directoryPath);
            return await operation();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectoryPath);
        }
    }

    private static async Task ConfigureGitIdentityAsync(string repositoryPath)
    {
        await RunGitAsync(repositoryPath, "config user.name \"Prompt Integration Tests\"");
        await RunGitAsync(repositoryPath, "config user.email \"prompt-integration-tests@example.com\"");
    }

    private static async Task<string> RunGitAsync(string workingDirectoryPath, string arguments)
    {
        var commandResult = await RunGitAllowFailureAsync(workingDirectoryPath, arguments);
        if (commandResult.ExitCode is not  0)
        {
            throw new InvalidOperationException($"git {arguments} failed in {workingDirectoryPath}: {commandResult.StandardError}");
        }

        return commandResult.StandardOutput;
    }

    private static async Task<GitCommandResult> RunGitAllowFailureAsync(string workingDirectoryPath, string arguments)
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

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
