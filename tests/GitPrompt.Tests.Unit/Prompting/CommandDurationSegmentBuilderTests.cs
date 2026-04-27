using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Tests.Unit.Prompting;

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

    [Theory]
    [InlineData(0, "0ms")]
    [InlineData(42, "42ms")]
    [InlineData(999, "999ms")]
    [InlineData(1000, "1.00s")]
    [InlineData(1500, "1.50s")]
    [InlineData(12345, "12.35s")]
    [InlineData(120000, "120.00s")]
    public void FormatDuration_ShouldRenderCorrectUnitAndPrecision(long ms, string expected)
    {
        // Act & Assert
        CommandDurationSegmentBuilder.FormatDuration(ms).Should().Be(expected);
    }
}
