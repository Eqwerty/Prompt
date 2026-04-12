using FluentAssertions;
using Prompt.Constants;
using Prompt.Git;
using static Prompt.Constants.PromptColors;
using static Prompt.Tests.Unit.Git.TestHelpers;

namespace Prompt.Tests.Unit.Git;

public sealed class GitStatusDisplayFormatterTests
{
    [Fact]
    public async Task BuildDisplay_WhenRepositoryHasCountsAndOperation_ShouldIncludeAllIndicators()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "merge\n");

        var statusCounts = new GitStatusCounts(
            StagedRenamed: 1,
            UnstagedModified: 1,
            Untracked: 1,
            Conflicts: 1);

        // Act
        var gitStatusDisplay =
            GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
                commitsAhead: 4,
                commitsBehind: 2,
                stashEntryCount: 2,
                statusCounts,
                gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain(BranchLabelWithOperation(TrackedBranchLabel("main"), "MERGE"));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconAhead, 4));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconBehind, 2));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconModified, 1));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconRenamed, 1));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconUntracked, 1));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconStash, 2));
        gitStatusDisplay.Should().Contain(Indicator(PromptIcons.IconConflicts, 1));
    }

    [Fact]
    public async Task BuildDisplay_WhenAllIndicatorTypesExist_ShouldRenderIndicatorsInExpectedOrder()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "merge\n");

        var statusCounts = new GitStatusCounts(
            StagedAdded: 1,
            StagedModified: 2,
            StagedDeleted: 4,
            StagedRenamed: 3,
            UnstagedAdded: 5,
            UnstagedModified: 6,
            UnstagedDeleted: 8,
            UnstagedRenamed: 7,
            Untracked: 9,
            Conflicts: 10);

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
            commitsAhead: 12,
            commitsBehind: 13,
            stashEntryCount: 2,
            statusCounts,
            gitDirectory.DirectoryPath);

        // Assert
        AssertInOrder(
            gitStatusDisplay,
            BranchLabelWithOperation(TrackedBranchLabel("main"), "MERGE"),
            Indicator(PromptIcons.IconAhead, 12),
            Indicator(PromptIcons.IconBehind, 13),
            Indicator(PromptIcons.IconAdded, 1),
            Indicator(PromptIcons.IconModified, 2),
            Indicator(PromptIcons.IconRenamed, 3),
            Indicator(PromptIcons.IconDeleted, 4),
            Indicator(PromptIcons.IconAdded, 5),
            Indicator(PromptIcons.IconModified, 6),
            Indicator(PromptIcons.IconRenamed, 7),
            Indicator(PromptIcons.IconDeleted, 8),
            Indicator(PromptIcons.IconUntracked, 9),
            Indicator(PromptIcons.IconConflicts, 10),
            Indicator(PromptIcons.IconStash, 2));
    }

    [Theory]
    [InlineData("MERGE_HEAD", "MERGE")]
    [InlineData("CHERRY_PICK_HEAD", "CHERRY-PICK")]
    public async Task BuildDisplay_WhenNoUpstreamBranchHasOperation_ShouldRenderOperationInsideBranchLabel(
        string operationMarkerFileName,
        string expectedOperationMarker)
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(gitDirectory.DirectoryPath, operationMarkerFileName), "head\n");

        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(NoUpstreamBranchLabel("feature"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            statusCounts,
            gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain(BranchLabelWithOperation(NoUpstreamBranchLabel("feature"), expectedOperationMarker));
    }

    [Fact]
    public void BuildDisplay_WhenBranchIsTracked_ShouldUseTrackedBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay =
            GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
                commitsAhead: 0,
                commitsBehind: 0,
                stashEntryCount: 0,
                statusCounts,
                gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith(Colored(ColorBranch, TrackedBranchLabel("main")));
    }

    [Fact]
    public void BuildDisplay_WhenBranchHasNoUpstream_ShouldUseNoUpstreamBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(NoUpstreamBranchLabel("feature"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            statusCounts,
            gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith(Colored(ColorBranchNoUpstream, NoUpstreamBranchLabel("feature")));
    }

    [Fact]
    public void BuildDisplay_WhenAheadBehindCountsExist_ShouldUseAheadAndBehindColors()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay =
            GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
                commitsAhead: 2,
                commitsBehind: 3,
                stashEntryCount: 0,
                statusCounts,
                gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain($" {Colored(ColorAhead, Indicator(PromptIcons.IconAhead, 2))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorBehind, Indicator(PromptIcons.IconBehind, 3))}");
    }

    [Fact]
    public void BuildDisplay_WhenMultipleSegmentsAreRendered_ShouldResetColorBetweenSegments()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        var statusCounts = new GitStatusCounts(
            StagedAdded: 1,
            StagedRenamed: 1,
            UnstagedModified: 1,
            Untracked: 1,
            Conflicts: 1);

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
            commitsAhead: 1,
            commitsBehind: 1,
            stashEntryCount: 0,
            statusCounts,
            gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain(Colored(ColorBranch, TrackedBranchLabel("main")));
        gitStatusDisplay.Should().Contain($" {Colored(ColorAhead, Indicator(PromptIcons.IconAhead, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorBehind, Indicator(PromptIcons.IconBehind, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorStaged, Indicator(PromptIcons.IconAdded, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorUnstaged, Indicator(PromptIcons.IconModified, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorUntracked, Indicator(PromptIcons.IconUntracked, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorState, Indicator(PromptIcons.IconConflicts, 1))}");
    }
}
