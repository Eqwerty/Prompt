using FluentAssertions;
using GitPrompt.Configuration;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigInitializerTests
{
    [Fact]
    public void EnsureConfigFileExists_WhenFileDoesNotExist_ShouldCreateDirectoryAndWriteDefaultContent()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.jsonc");

        // Act
        ConfigInitializer.EnsureConfigFileExists(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(ConfigInitializer.BuildDefaultConfigContent());

        File.Delete(configPath);
        Directory.Delete(Path.GetDirectoryName(configPath)!);
    }

    [Fact]
    public void EnsureConfigFileExists_WhenFileAlreadyExists_ShouldNotOverwriteContent()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        const string originalContent = "custom content";
        File.WriteAllText(configPath, originalContent);

        // Act
        ConfigInitializer.EnsureConfigFileExists(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(originalContent);

        File.Delete(configPath);
    }

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
    public void BuildDefaultConfigContent_ShouldIncludeShowUser()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("showUser");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderShowUserAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().ShowUser ? "true" : "false";

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"showUser\": {expectedValue}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldIncludeShowHost()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("showHost");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderShowHostAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().ShowHost ? "true" : "false";

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"showHost\": {expectedValue}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldIncludeMaxPathDepth()
    {
        // Act & Assert
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("maxPathDepth");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderMaxPathDepthAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().MaxPathDepth.ToString();

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"maxPathDepth\": {expectedValue}");
    }
}
