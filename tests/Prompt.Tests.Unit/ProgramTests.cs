using FluentAssertions;

namespace Prompt.Tests.Unit;

public sealed class ProgramTests
{
    [Fact]
    public void GetPromptSymbol_WhenOnWindows_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: true, user: "root"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, user: "root"));

        symbol.Should().Be("#");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, user: "me"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndWindowsUsernameIsRoot_ShouldReturnHash()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, windowsUserName: "root"));

        symbol.Should().Be("#");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndNoUserSet_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false));

        symbol.Should().Be("$");
    }

    [Fact]
    public async Task BuildGitStatusDisplay_WhenCountsAndOperationExist_ShouldIncludeReadableIndicators()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(stashLogDirectoryPath, "stash"), "entry-1\nentry-2\n");
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "merge\n");

        var statusCounts = new StatusCounts(
            stagedRenamed: 1,
            unstagedModified: 1,
            untracked: 1,
            conflicts: 1);

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 4, 2, statusCounts, gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain("(main|MERGE)");
        gitStatusDisplay.Should().Contain("↑4");
        gitStatusDisplay.Should().Contain("↓2");
        gitStatusDisplay.Should().Contain("~1");
        gitStatusDisplay.Should().Contain("→1");
        gitStatusDisplay.Should().Contain("?1");
        gitStatusDisplay.Should().Contain("@2");
        gitStatusDisplay.Should().Contain("!1");
    }

    [Theory]
    [InlineData("MERGE_HEAD", "MERGE")]
    [InlineData("CHERRY_PICK_HEAD", "CHERRY-PICK")]
    public async Task BuildGitStatusDisplay_WhenNoUpstreamBranchHasOperation_ShouldPlaceOperationInsideBranchLabel(
        string operationMarkerFileName,
        string expectedOperationMarker)
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, operationMarkerFileName), "head\n");

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("*(feature)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain($"*(feature|{expectedOperationMarker})");
    }

    [Fact]
    public async Task ResolveRebaseBranchName_WhenRebaseHeadNameFileExists_ShouldReturnBranchName()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var rebaseDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "rebase-merge");
        Directory.CreateDirectory(rebaseDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(rebaseDirectoryPath, "head-name"), "refs/heads/feature\n");

        // Act
        var rebaseBranchName = Program.ResolveRebaseBranchName(gitDirectory.DirectoryPath);

        // Assert
        rebaseBranchName.Should().Be("feature");
    }

    [Fact]
    public async Task FindMatchingRemoteReferences_WhenLooseAndPackedRefsContainMatches_ShouldReturnMatchingReferences()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var remoteDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "refs", "remotes", "origin");
        Directory.CreateDirectory(remoteDirectoryPath);

        await File.WriteAllTextAsync(Path.Combine(remoteDirectoryPath, "main"), "abcdef1234567890\n");
        await File.WriteAllTextAsync(
            Path.Combine(gitDirectory.DirectoryPath, "packed-refs"),
            """
            # pack-refs with: peeled fully-peeled sorted
            abcdef1234567890 refs/remotes/origin/release
            1111111111111111 refs/remotes/origin/other
            """
        );

        // Act
        var matchingRemoteReferences = Program.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().Contain("origin/main");
        matchingRemoteReferences.Should().Contain("origin/release");
        matchingRemoteReferences.Should().NotContain("origin/other");
    }

    [Fact]
    public async Task ReadStashEntryCount_WhenStashLogContainsEntries_ShouldCountStashLines()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(stashLogDirectoryPath, "stash"), "first\nsecond\nthird\n");

        // Act
        var stashEntryCount = Program.ReadStashEntryCount(gitDirectory.DirectoryPath);

        // Assert
        stashEntryCount.Should().Be(3);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_WhenInputVaries_ShouldQuoteOnlyWhenNecessary(string value, string expected)
    {
        // Arrange
        // Inline data provides the test inputs.

        // Act
        var escapedValue = Program.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_WhenInputContainsBackslashesAndQuotes_ShouldEscapeCharactersInsideQuotedArgument()
    {
        // Arrange
        const string value = "C:\\Program Files\\My \"App\"";

        // Act
        var escapedValue = Program.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("123456", "123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("1234567890", "1234567")]
    public void ShortenObjectId_WhenInputVaries_ShouldReturnExpectedShortForm(string objectId, string expectedShortObjectId)
    {
        // Arrange
        // Inline data provides the test inputs.

        // Act
        var shortObjectId = Program.ShortenObjectId(objectId);

        // Assert
        shortObjectId.Should().Be(expectedShortObjectId);
    }

    [Fact]
    public void ReadGitOperationMarker_WhenNoOperationMarkerExists_ShouldReturnEmpty()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadGitOperationMarker_WhenCherryPickHeadExists_ShouldReturnCherryPick()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "CHERRY_PICK_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("CHERRY-PICK");
    }

    [Fact]
    public async Task ReadGitOperationMarker_WhenRevertHeadExists_ShouldReturnRevert()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "REVERT_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("REVERT");
    }

    [Fact]
    public async Task ReadGitOperationMarker_WhenBisectLogExists_ShouldReturnBisect()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "BISECT_LOG"), "bisect\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("BISECT");
    }

    [Fact]
    public async Task ReadGitOperationMarker_WhenRebaseAndOtherMarkersExist_ShouldPrioritizeRebase()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(gitDirectory.DirectoryPath, "rebase-merge"));
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "head\n");
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "CHERRY_PICK_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("REBASE");
    }

    [Fact]
    public void ResolveGitDirectoryPath_WhenDotGitPathIsDirectory_ShouldReturnDotGitDirectoryPath()
    {
        // Arrange
        using var repoDirectory = new TemporaryDirectory();
        var dotGitPath = Path.Combine(repoDirectory.DirectoryPath, ".git");
        Directory.CreateDirectory(dotGitPath);

        // Act
        var resolvedGitDirectoryPath = Program.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(dotGitPath);
    }

    [Fact]
    public async Task ResolveGitDirectoryPath_WhenGitdirFileContainsRelativePath_ShouldResolveAbsoluteGitDirectoryPath()
    {
        // Arrange
        using var rootDirectory = new TemporaryDirectory();
        var actualGitDirectoryPath = Path.Combine(rootDirectory.DirectoryPath, "actual-git");
        var workingTreePath = Path.Combine(rootDirectory.DirectoryPath, "worktree");
        Directory.CreateDirectory(actualGitDirectoryPath);
        Directory.CreateDirectory(workingTreePath);

        var dotGitPath = Path.Combine(workingTreePath, ".git");
        await File.WriteAllTextAsync(dotGitPath, "gitdir: ../actual-git\n");

        // Act
        var resolvedGitDirectoryPath = Program.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(Path.GetFullPath(actualGitDirectoryPath));
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenTrackedBranchLabelIsRendered_ShouldUseTrackedBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith("\u0001\e[1;36m\u0002(main)\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenNoUpstreamBranchLabelIsRendered_ShouldUseNoUpstreamBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("*(feature)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith("\u0001\e[1;36m\u0002*(feature)\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenAheadBehindCountsAreRendered_ShouldUseAheadAndBehindColors()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 2, 3, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↑2\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↓3\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenMultipleSegmentsAreRendered_ShouldResetColorAfterEachSegment()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        var statusCounts = new StatusCounts(
            stagedAdded: 1,
            unstagedModified: 1,
            untracked: 1,
            conflicts: 1);

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 1, 1, statusCounts, gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain("\u0001\e[1;36m\u0002(main)\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↑1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↓1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;32m\u0002+1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;31m\u0002~1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;31m\u0002?1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;31m\u0002!1\u0001\e[0m\u0002");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Unit", Guid.NewGuid().ToString("N"));
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
}
