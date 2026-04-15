using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusMergeOperationIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenMergeIsInProgress_ShouldShowMergeOperationMarker()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act
        var mergeCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "merge feature");
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|MERGE");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenCherryPickIsInProgress_ShouldShowCherryPickOperationMarker()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act
        var cherryPickCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|CHERRY-PICK");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsMerging_ShouldShowMergeOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b other main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "other\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"other change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout feature");

        // Act
        var mergeCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "merge other");
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.NoUpstreamBranchLabel("feature"), "MERGE"));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsCherryPicking_ShouldShowCherryPickOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        // Act
        var cherryPickCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.NoUpstreamBranchLabel("feature"), "CHERRY-PICK"));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRebaseIsInProgress_ShouldShowBranchNameInsteadOfDetachedCommit()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout feature");

        // Act
        var rebaseCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "rebase main");
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(repositoryPath);

        // Assert
        rebaseCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("feature|REBASE");
        gitStatusSegment.Should().NotContain("...|REBASE");
    }
}
