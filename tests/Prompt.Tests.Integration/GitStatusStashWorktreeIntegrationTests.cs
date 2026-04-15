using FluentAssertions;
using Prompt.Constants;
using Prompt.Git;

namespace Prompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusStashWorktreeIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenStashExists_ShouldShowStashMarker()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add tracked.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "changed\n");
        await TestHelpers.RunGitAsync(repositoryPath, "stash push -m \"wip\"");

        // Act
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconStash, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsInWorktree_ShouldShowNoUpstreamMarkerAndAheadCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");
        var worktreePath = Path.Combine(sandbox.DirectoryPath, "feature-worktree");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, $"worktree add -b feature {TestHelpers.Quote(worktreePath)}");

        await File.WriteAllTextAsync(Path.Combine(worktreePath, "feature.txt"), "feature\n");
        await TestHelpers.RunGitAsync(worktreePath, "add feature.txt");
        await TestHelpers.RunGitAsync(worktreePath, "commit -m \"feature commit\"");

        // Act
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(worktreePath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("feature"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 1));
    }
}
