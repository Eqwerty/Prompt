using FluentAssertions;
using GitPrompt.Configuration;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigInitializerTests
{
    [Fact]
    public void BuildDefaultConfigContent_ShouldIncludeCommandTimeoutMs()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("commandTimeoutMs");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderCommandTimeoutMsAsDefaultValue()
    {
        // Arrange
        var expectedMs = (long)(new Config().CommandTimeout?.TotalMilliseconds ?? 0);

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"commandTimeoutMs\": {expectedMs}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldNotContainUnresolvedPlaceholders()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — check that known placeholder tokens are not present
        content.Should().NotMatchRegex(@"\{[a-zA-Z]+\}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldIncludeShowCommandDuration()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("showCommandDuration");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderShowCommandDurationAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().ShowCommandDuration ? "true" : "false";

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"showCommandDuration\": {expectedValue}");
    }
}
