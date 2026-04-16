using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitRepositoryLocatorTests
{
    [Fact]
    public void FindRepositoryContext_WhenDirectoryIsInsideRepository_ShouldReturnWorkingTreeAndGitDirectory()
    {
        // Arrange
        using var repoDirectory = new TemporaryDirectory();
        var dotGitPath = Path.Combine(repoDirectory.DirectoryPath, ".git");
        Directory.CreateDirectory(dotGitPath);

        var nestedPath = Path.Combine(repoDirectory.DirectoryPath, "src", "feature");
        Directory.CreateDirectory(nestedPath);

        // Act
        var repositoryContext = GitRepositoryLocator.FindRepositoryContext(nestedPath);

        // Assert
        repositoryContext.Should().NotBeNull();
        repositoryContext.Value.WorkingTreePath.Should().Be(Path.GetFullPath(repoDirectory.DirectoryPath));
        repositoryContext.Value.GitDirectoryPath.Should().Be(Path.GetFullPath(dotGitPath));
    }

    [Fact]
    public void FindRepositoryContext_WhenCachedRepositoryBecomesInvalid_ShouldReturnNull()
    {
        // Arrange
        using var repoDirectory = new TemporaryDirectory();
        var dotGitPath = Path.Combine(repoDirectory.DirectoryPath, ".git");
        Directory.CreateDirectory(dotGitPath);

        var nestedPath = Path.Combine(repoDirectory.DirectoryPath, "src");
        Directory.CreateDirectory(nestedPath);

        var firstLookup = GitRepositoryLocator.FindRepositoryContext(nestedPath);
        firstLookup.Should().NotBeNull();

        Directory.Delete(dotGitPath, recursive: true);

        // Act
        var secondLookup = GitRepositoryLocator.FindRepositoryContext(nestedPath);

        // Assert
        secondLookup.Should().BeNull();
    }

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
