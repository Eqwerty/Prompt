using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Constants;

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
    public void BuildDefaultConfigContent_ShouldRenderMaxPathDepthAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().MaxPathDepth.ToString();

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"maxPathDepth\": {expectedValue}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderMultilinePromptAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().MultilinePrompt ? "true" : "false";

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"multilinePrompt\": {expectedValue}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderNewlineBeforePromptAsDefaultValue()
    {
        // Arrange
        var expectedValue = new Config().NewlineBeforePrompt ? "true" : "false";

        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"\"newlineBeforePrompt\": {expectedValue}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderPromptSymbolAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"promptSymbol\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllIconValuesAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — all icons default to null
        content.Should().Contain("\"ahead\": null");
        content.Should().Contain("\"behind\": null");
        content.Should().Contain("\"added\": null");
        content.Should().Contain("\"modified\": null");
        content.Should().Contain("\"renamed\": null");
        content.Should().Contain("\"deleted\": null");
        content.Should().Contain("\"untracked\": null");
        content.Should().Contain("\"conflicts\": null");
        content.Should().Contain("\"stash\": null");
        content.Should().Contain("\"noUpstreamMarker\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderIconDefaultGlyphsFromPromptIcons()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — comment glyphs come from PromptIcons constants
        content.Should().Contain($"null = default: {PromptIcons.IconAhead}");
        content.Should().Contain($"null = default: {PromptIcons.IconBehind}");
        content.Should().Contain($"null = default: {PromptIcons.IconAdded}");
        content.Should().Contain($"null = default: {PromptIcons.IconModified}");
        content.Should().Contain($"null = default: {PromptIcons.IconRenamed}");
        content.Should().Contain($"null = default: {PromptIcons.IconDeleted}");
        content.Should().Contain($"null = default: {PromptIcons.IconUntracked}");
        content.Should().Contain($"null = default: {PromptIcons.IconConflicts}");
        content.Should().Contain($"null = default: {PromptIcons.IconStash}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderNoUpstreamMarkerDefaultGlyph()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"null = default: {BranchLabelTokens.NoUpstreamBranchMarker}");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllColorValuesAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — all color slots default to null
        content.Should().Contain("\"user\": null,");
        content.Should().Contain("\"host\": null,");
        content.Should().Contain("\"path\": null,");
        content.Should().Contain("\"commandDuration\": null,");
        content.Should().Contain("\"branch\": null,");
        content.Should().Contain("\"branchNoUpstream\": null,");
        content.Should().Contain("\"ahead\": null,");
        content.Should().Contain("\"behind\": null,");
        content.Should().Contain("\"staged\": null,");
        content.Should().Contain("\"unstaged\": null,");
        content.Should().Contain("\"untracked\": null,");
        content.Should().Contain("\"stash\": null,");
        content.Should().Contain("\"conflict\": null,");
        content.Should().Contain("\"missingPath\": null,");
        content.Should().Contain("\"timeout\": null,");
        content.Should().Contain("\"promptSymbol\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderColorDefaultHexFromAnsiColors()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — comment hex values come from AnsiColors constants
        content.Should().Contain($"null = default: {AnsiColors.Green}");
        content.Should().Contain($"null = default: {AnsiColors.Magenta}");
        content.Should().Contain($"null = default: {AnsiColors.Orange}");
        content.Should().Contain($"null = default: {AnsiColors.Blue}");
        content.Should().Contain($"null = default: {AnsiColors.Red}");
        content.Should().Contain($"null = default: {AnsiColors.BoldRed}");
        content.Should().Contain($"null = default: {AnsiColors.Yellow}");
        content.Should().Contain($"null = default: {AnsiColors.LightGray}");
    }
}
