using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusRepositoryStateIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenCurrentDirectoryIsNotInGitRepository_ShouldReturnEmpty()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();

        // Act
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(sandbox.DirectoryPath);

        // Assert
        gitStatusSegment.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenExecutedFromNestedRepositoryDirectory_ShouldFindGitDirectoryInParent()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");
        var nestedDirectoryPath = Path.Combine(repositoryPath, "src", "features");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        Directory.CreateDirectory(nestedDirectoryPath);

        // Act
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(nestedDirectoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.TrackedBranchLabel("main"));
    }
}
