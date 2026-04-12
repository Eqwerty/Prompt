using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Unit.Git;

public sealed class GitRepositoryLocatorTests
{
    [Fact]
    public void ResolveGitDirectoryPath_WhenDotGitPathIsDirectory_ShouldReturnSameDirectoryPath()
    {
        // Arrange
        using var repoDirectory = new TemporaryDirectory();
        var dotGitPath = Path.Combine(repoDirectory.DirectoryPath, ".git");
        Directory.CreateDirectory(dotGitPath);

        // Act
        var resolvedGitDirectoryPath = GitRepositoryLocator.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(dotGitPath);
    }

    [Fact]
    public async Task ResolveGitDirectoryPath_WhenGitdirFileContainsRelativePath_ShouldResolveAbsolutePath()
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
        var resolvedGitDirectoryPath = GitRepositoryLocator.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(Path.GetFullPath(actualGitDirectoryPath));
    }
}
