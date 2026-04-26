using FluentAssertions;
using GitPrompt.Constants;
using GitPrompt.Git;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Tests.Unit.Git.TestHelpers;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitStatusDisplayFormatterTests
{
    [Fact]
    public void BuildDisplay_WhenRepositoryHasCountsAndOperation_ShouldIncludeAllIndicators()
    {
        // Arrange
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
                operationName: "MERGE");

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
    public void BuildDisplay_WhenAllIndicatorTypesExist_ShouldRenderIndicatorsInExpectedOrder()
    {
        // Arrange
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
            operationName: "MERGE");

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
    [InlineData("MERGE")]
    [InlineData("CHERRY-PICK")]
    public void BuildDisplay_WhenNoUpstreamBranchHasOperation_ShouldRenderOperationInsideBranchLabel(
        string operationName)
    {
        // Arrange
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(NoUpstreamBranchLabel("feature"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            statusCounts,
            operationName);

        // Assert
        gitStatusDisplay.Should().Contain(BranchLabelWithOperation(NoUpstreamBranchLabel("feature"), operationName));
    }

    [Fact]
    public void BuildDisplay_WhenBranchIsTracked_ShouldUseTrackedBranchColor()
    {
        // Arrange
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay =
            GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
                commitsAhead: 0,
                commitsBehind: 0,
                stashEntryCount: 0,
                statusCounts,
                operationName: string.Empty);

        // Assert
        gitStatusDisplay.Should().StartWith(Colored(ColorBranch, TrackedBranchLabel("main")));
    }

    [Fact]
    public void BuildDisplay_WhenBranchHasNoUpstream_ShouldUseNoUpstreamBranchColor()
    {
        // Arrange
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay = GitStatusDisplayFormatter.BuildDisplay(NoUpstreamBranchLabel("feature"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            statusCounts,
            operationName: string.Empty);

        // Assert
        gitStatusDisplay.Should().StartWith(Colored(ColorBranchNoUpstream, NoUpstreamBranchLabel("feature")));
    }

    [Fact]
    public void BuildDisplay_WhenAheadBehindCountsExist_ShouldUseAheadAndBehindColors()
    {
        // Arrange
        var statusCounts = new GitStatusCounts();

        // Act
        var gitStatusDisplay =
            GitStatusDisplayFormatter.BuildDisplay(TrackedBranchLabel("main"),
                commitsAhead: 2,
                commitsBehind: 3,
                stashEntryCount: 0,
                statusCounts,
                operationName: string.Empty);

        // Assert
        gitStatusDisplay.Should().Contain($" {Colored(ColorAhead, Indicator(PromptIcons.IconAhead, 2))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorBehind, Indicator(PromptIcons.IconBehind, 3))}");
    }

    [Fact]
    public void BuildDisplay_WhenMultipleSegmentsAreRendered_ShouldResetColorBetweenSegments()
    {
        // Arrange
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
            operationName: string.Empty);

        // Assert
        gitStatusDisplay.Should().Contain(Colored(ColorBranch, TrackedBranchLabel("main")));
        gitStatusDisplay.Should().Contain($" {Colored(ColorAhead, Indicator(PromptIcons.IconAhead, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorBehind, Indicator(PromptIcons.IconBehind, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorStaged, Indicator(PromptIcons.IconAdded, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorUnstaged, Indicator(PromptIcons.IconModified, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorUntracked, Indicator(PromptIcons.IconUntracked, 1))}");
        gitStatusDisplay.Should().Contain($" {Colored(ColorConflict, Indicator(PromptIcons.IconConflicts, 1))}");
    }
}
