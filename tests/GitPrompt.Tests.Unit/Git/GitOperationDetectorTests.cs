using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitOperationDetectorTests
{
    [Fact]
    public async Task ResolveRebaseBranchName_WhenRebaseHeadNameFileExists_ShouldReturnHeadBranchName()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var rebaseDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "rebase-merge");
        Directory.CreateDirectory(rebaseDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(rebaseDirectoryPath, "head-name"), "refs/heads/feature\n");

        // Act
        var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectory.DirectoryPath);

        // Assert
        rebaseBranchName.Should().Be("feature");
    }

    [Fact]
    public async Task ResolveRebaseBranchName_WhenRebaseApplyHeadNameFileExists_ShouldReturnHeadBranchName()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var rebaseDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "rebase-apply");
        Directory.CreateDirectory(rebaseDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(rebaseDirectoryPath, "head-name"), "refs/heads/feature\n");

        // Act
        var rebaseBranchName = GitOperationDetector.ResolveRebaseBranchName(gitDirectory.DirectoryPath);

        // Assert
        rebaseBranchName.Should().Be("feature");
    }

    [Fact]
    public async Task FindMatchingRemoteReferences_WhenLooseAndPackedRefsContainMatches_ShouldReturnOnlyMatchingReferences()
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
        var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().Contain("origin/main");
        matchingRemoteReferences.Should().Contain("origin/release");
        matchingRemoteReferences.Should().NotContain("origin/other");
    }

    [Fact]
    public async Task FindMatchingRemoteReferences_WhenSameReferenceExistsLooseAndPacked_ShouldReturnDistinctReferenceOnce()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var remoteDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "refs", "remotes", "origin");
        Directory.CreateDirectory(remoteDirectoryPath);

        await File.WriteAllTextAsync(Path.Combine(remoteDirectoryPath, "main"), "abcdef1234567890\n");
        await File.WriteAllTextAsync(
            Path.Combine(gitDirectory.DirectoryPath, "packed-refs"),
            "abcdef1234567890 refs/remotes/origin/main\n");

        // Act
        var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().Equal("origin/main");
    }

    [Fact]
    public void ReadGitOperationMarker_WhenNoOperationMarkerExists_ShouldReturnEmptyMarker()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var operationMarker = GitOperationDetector.ReadGitOperationMarker(gitDirectory.DirectoryPath);

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
        var operationMarker = GitOperationDetector.ReadGitOperationMarker(gitDirectory.DirectoryPath);

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
        var operationMarker = GitOperationDetector.ReadGitOperationMarker(gitDirectory.DirectoryPath);

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
        var operationMarker = GitOperationDetector.ReadGitOperationMarker(gitDirectory.DirectoryPath);

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
        var operationMarker = GitOperationDetector.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("REBASE");
    }

    [Fact]
    public void FindMatchingRemoteReferences_WhenGitDirectoryPathIsNull_ShouldReturnEmpty()
    {
        // Act
        var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(null!, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().BeEmpty();
    }

    [Fact]
    public async Task FindMatchingRemoteReferences_WhenPackedRefsContainsMalformedLines_ShouldIgnoreMalformedAndReturnValidMatch()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(gitDirectory.DirectoryPath, "packed-refs"),
            """
            # pack-refs with: peeled fully-peeled sorted
            malformed-line-without-space
            abcdef1234567890
            abcdef1234567890 refs/heads/main
            ^abcdef1234567890
            abcdef1234567890 refs/remotes/origin/release
            """
        );

        // Act
        var matchingRemoteReferences = GitOperationDetector.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().Equal("origin/release");
    }
}
