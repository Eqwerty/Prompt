using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Git;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Tests.Unit.Git.TestHelpers;

namespace GitPrompt.Tests.Unit.Git;

[Collection(ConfigIsolationCollection.Name)]
public sealed class GitStatusDisplayFormatterCustomIconTests
{
    [Fact]
    public void BuildDisplay_WhenCustomAheadIconIsConfigured_ShouldUseCustomIcon()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { Ahead = "⬆" } });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplay(
            TrackedBranchLabel("main"),
            commitsAhead: 3,
            commitsBehind: 0,
            stashEntryCount: 0,
            new GitStatusCounts(),
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Indicator("⬆", 3));
        display.Should().NotContain(Indicator("↑", 3));
    }

    [Fact]
    public void BuildDisplay_WhenCustomBehindIconIsConfigured_ShouldUseCustomIcon()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { Behind = "⬇" } });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplay(
            TrackedBranchLabel("main"),
            commitsAhead: 0,
            commitsBehind: 2,
            stashEntryCount: 0,
            new GitStatusCounts(),
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Indicator("⬇", 2));
        display.Should().NotContain(Indicator("↓", 2));
    }

    [Fact]
    public void BuildDisplay_WhenMultipleCustomIconsAreConfigured_ShouldUseAllCustomIcons()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config
        {
            Icons = new Config.IconsConfig
            {
                Ahead = "A",
                Behind = "B",
                Added = "N",
                Modified = "M",
                Untracked = "U",
                Stash = "S",
            }
        });
        var statusCounts = new GitStatusCounts(StagedAdded: 1, UnstagedModified: 1, Untracked: 1);

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplay(
            TrackedBranchLabel("main"),
            commitsAhead: 2,
            commitsBehind: 1,
            stashEntryCount: 3,
            statusCounts,
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Indicator("A", 2));
        display.Should().Contain(Indicator("B", 1));
        display.Should().Contain(Indicator("N", 1));
        display.Should().Contain(Indicator("M", 1));
        display.Should().Contain(Indicator("U", 1));
        display.Should().Contain(Indicator("S", 3));
    }

    [Fact]
    public void BuildDisplay_WhenCustomIconIsEmptyString_ShouldShowEmptyIconWithCount()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { Ahead = "" } });

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplay(
            TrackedBranchLabel("main"),
            commitsAhead: 5,
            commitsBehind: 0,
            stashEntryCount: 0,
            new GitStatusCounts(),
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Indicator("", 5));
        display.Should().NotContain(Indicator("↑", 5));
    }

    [Fact]
    public void BuildDisplay_WhenCustomNoUpstreamMarkerIsConfigured_ShouldUseThatMarkerInBranchLabel()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { NoUpstreamMarker = "!" } });

        // Act
        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel("main", hasUpstream: false);

        // Assert
        branchLabel.Should().StartWith("!");
        branchLabel.Should().NotStartWith("*");
    }

    [Fact]
    public void BuildDisplay_WhenCustomNoUpstreamMarkerIsConfigured_ShouldApplyNoUpstreamColor()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig { NoUpstreamMarker = "!" } });
        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel("main", hasUpstream: false);

        // Act
        var display = GitStatusDisplayFormatter.BuildDisplay(
            branchLabel,
            commitsAhead: 0,
            commitsBehind: 0,
            stashEntryCount: 0,
            new GitStatusCounts(),
            operationName: string.Empty);

        // Assert
        display.Should().Contain(Colored(ColorBranchNoUpstream, branchLabel));
    }

    [Fact]
    public void BuildDisplay_WhenNoUpstreamMarkerIsAbsent_ShouldDefaultToAsterisk()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Icons = new Config.IconsConfig() });

        // Act
        var branchLabel = GitStatusDisplayFormatter.BuildBranchLabel("main", hasUpstream: false);

        // Assert
        branchLabel.Should().StartWith("*");
    }
}
