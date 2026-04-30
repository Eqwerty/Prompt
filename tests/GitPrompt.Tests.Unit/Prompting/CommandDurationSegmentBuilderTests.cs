using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Tests.Unit.Prompting;

[Collection(ConfigIsolationCollection.Name)]
public sealed class CommandDurationSegmentBuilderTests
{
    [Fact]
    public void Build_WhenShowCommandDurationIsFalse_ShouldReturnEmpty()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 123);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = false });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().BeEmpty();
    }

    [Fact]
    public void Build_WhenShowCommandDurationIsTrueAndDurationIsNull_ShouldReturnEmpty()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: null);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().BeEmpty();
    }

    [Fact]
    public void Build_WhenShowCommandDurationIsTrueAndDurationIsPresent_ShouldReturnColoredSegment()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 42);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorCommandDuration}42ms{ColorReset}");
    }

    [Fact]
    public void Build_WhenDurationIsZero_ShouldRenderZeroMs()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 0);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().Be($"{ColorCommandDuration}0ms{ColorReset}");
    }

    [Fact]
    public void Build_WhenDurationExceedsMinimumThreshold_ShouldReturnSegment()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 5000);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true, CommandDurationMinMs = 2000 });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().NotBeEmpty();
        segment.Should().Contain("5.0s");
    }

    [Fact]
    public void Build_WhenDurationIsBelowMinimumThreshold_ShouldReturnEmpty()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 500);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true, CommandDurationMinMs = 2000 });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().BeEmpty();
    }

    [Fact]
    public void Build_WhenDurationEqualsMinimumThreshold_ShouldReturnSegment()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 2000);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true, CommandDurationMinMs = 2000 });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_WhenMinimumThresholdIsNull_ShouldAlwaysShowDuration()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 1);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true, CommandDurationMinMs = null });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_WhenMinimumThresholdIsZero_ShouldAlwaysShowDuration()
    {
        // Arrange
        var platformProvider = new TestPlatformProvider(lastCommandDurationMs: 1);
        using var _ = ConfigReader.OverrideForTesting(new Config { ShowCommandDuration = true, CommandDurationMinMs = 0 });

        // Act
        var segment = CommandDurationSegmentBuilder.Build(platformProvider);

        // Assert
        segment.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0, "0ms")]
    [InlineData(42, "42ms")]
    [InlineData(999, "999ms")]
    [InlineData(1000, "1.0s")]
    [InlineData(1100, "1.1s")]
    [InlineData(1500, "1.5s")]
    [InlineData(1950, "1.9s")]
    [InlineData(12345, "12.3s")]
    [InlineData(59999, "59.9s")]
    [InlineData(60000, "1m0s")]
    [InlineData(136000, "2m16s")]
    [InlineData(120000, "2m0s")]
    [InlineData(3599999, "59m59s")]
    [InlineData(3600000, "1h0m0s")]
    [InlineData(3724000, "1h2m4s")]
    public void FormatDuration_ShouldRenderCorrectUnitAndPrecision(long ms, string expected)
    {
        // Act & Assert
        CommandDurationSegmentBuilder.FormatDuration(ms).Should().Be(expected);
    }
}
