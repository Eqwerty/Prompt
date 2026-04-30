using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Constants;
using GitPrompt.Git;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Tests.Unit.Git.TestHelpers;

namespace GitPrompt.Tests.Unit.Git;

[Collection(ConfigIsolationCollection.Name)]
public sealed class GitStatusDisplayFormatterCompactModeTests : IDisposable
{
    private readonly IDisposable _configOverride = ConfigReader.OverrideForTesting(new Config());

    public void Dispose() => _configOverride.Dispose();

    [Fact]
    public void BuildDisplayCompact_WhenRepoIsDirty_ShouldShowDirtyIcon()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: true,
            operationName: string.Empty);

        // Assert
        display.Should().Contain(PromptIcons.IconDirty.ToString());
        display.Should().NotContain(PromptIcons.IconClean.ToString());
    }

    [Fact]
    public void BuildDisplayCompact_WhenRepoIsClean_ShouldShowCleanIcon()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().Contain(PromptIcons.IconClean.ToString());
        display.Should().NotContain(PromptIcons.IconDirty.ToString());
    }

    [Fact]
    public void BuildDisplayCompact_WhenRepoIsDirty_ShouldUseDirtyColor()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: true,
            operationName: string.Empty);

        // Assert
        display.Should().Contain($" {Colored(ColorDirty, PromptIcons.IconDirty.ToString())}");
    }

    [Fact]
    public void BuildDisplayCompact_WhenRepoIsClean_ShouldUseCleanColor()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().Contain($" {Colored(ColorClean, PromptIcons.IconClean.ToString())}");
    }

    [Fact]
    public void BuildDisplayCompact_WhenStashExistsAndShowStashIsTrue_ShouldShowStashCount()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowStashInCompactMode = true });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 3,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Indicator(PromptIcons.IconStash, 3));
    }

    [Fact]
    public void BuildDisplayCompact_WhenStashExistsAndShowStashIsFalse_ShouldOmitStashCount()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowStashInCompactMode = false });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 3,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().NotContain(Indicator(PromptIcons.IconStash, 3));
    }

    [Fact]
    public void BuildDisplayCompact_ShouldNotRenderGranularStatusCounts()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: true,
            operationName: string.Empty);

        // Assert — none of the verbose-only count icons should appear (with a number)
        display.Should().NotMatchRegex($@"\{PromptIcons.IconAdded}\d");
        display.Should().NotMatchRegex($@"\{PromptIcons.IconModified}\d");
        display.Should().NotMatchRegex($@"\{PromptIcons.IconRenamed}\d");
        display.Should().NotMatchRegex($@"\{PromptIcons.IconDeleted}\d");
        display.Should().NotMatchRegex($@"\{PromptIcons.IconUntracked}\d");
        display.Should().NotMatchRegex($@"\{PromptIcons.IconConflicts}\d");
    }

    [Fact]
    public void BuildDisplayCompact_WhenAllIndicatorsPresent_ShouldRenderInExpectedOrder()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowStashInCompactMode = true });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 3,
            commitsBehind: 1,
            stashEntryCount: 2,
            isDirty: true,
            operationName: string.Empty);

        // Assert
        AssertInOrder(
            display,
            TrackedBranchLabel("main"),
            Indicator(PromptIcons.IconAhead, 3),
            Indicator(PromptIcons.IconBehind, 1),
            PromptIcons.IconDirty.ToString(),
            Indicator(PromptIcons.IconStash, 2));
    }

    [Fact]
    public void BuildDisplayCompact_WhenCustomDirtyIconIsConfigured_ShouldUseCustomIcon()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { Dirty = "X" } });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: true,
            operationName: string.Empty);

        // Assert
        display.Should().Contain("X");
        display.Should().NotContain(PromptIcons.IconDirty.ToString());
    }

    [Fact]
    public void BuildDisplayCompact_WhenCustomCleanIconIsConfigured_ShouldUseCustomIcon()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { Clean = "OK" } });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().Contain("OK");
        display.Should().NotContain(PromptIcons.IconClean.ToString());
    }

    [Fact]
    public void BuildDisplayCompact_WhenBranchIsTracked_ShouldUseTrackedBranchColor()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().StartWith(Colored(ColorBranch, TrackedBranchLabel("main")));
    }

    [Fact]
    public void BuildDisplayCompact_WhenBranchHasNoUpstream_ShouldUseNoUpstreamBranchColor()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            NoUpstreamBranchLabel("feature"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: string.Empty);

        // Assert
        display.Should().StartWith(Colored(ColorBranchNoUpstream, NoUpstreamBranchLabel("feature")));
    }

    [Fact]
    public void BuildDisplayCompact_WhenOperationIsInProgress_ShouldIncludeOperationInBranchLabel()
    {
        // Act
        var display = GitStatusDisplayFormatter.BuildDisplayCompact(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            isDirty: false,
            operationName: "MERGE");

        // Assert
        display.Should().Contain(BranchLabelWithOperation(TrackedBranchLabel("main"), "MERGE"));
    }
}
