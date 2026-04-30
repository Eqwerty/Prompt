using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigInitializerTests
{
    [Fact]
    public void BuildConfigContent_WhenPassedCustomConfig_ShouldUseItsValuesInsteadOfDefaults()
    {
        // Arrange
        var customConfig = new Config
        {
            ShowUser = false,
            MaxPathDepth = 3,
            Compact = true
        };

        // Act
        var content = ConfigInitializer.BuildConfigContent(customConfig);

        // Assert
        content.Should().Contain("\"showUser\": false");
        content.Should().Contain("\"maxPathDepth\": 3");
        content.Should().Contain("\"compact\": true");
    }

    [Fact]
    public void BuildConfigContent_WhenPassedConfigWithCustomIcon_ShouldSerializeIconValue()
    {
        // Arrange
        var customConfig = new Config
        {
            Icons = new Config.IconsConfig { Ahead = "↑" }
        };

        // Act
        var content = ConfigInitializer.BuildConfigContent(customConfig);

        // Assert
        content.Should().Contain("\"ahead\": \"↑\"");
    }

    [Fact]
    public void BuildConfigContent_WhenPassedNewConfig_ShouldProduceSameOutputAsBuildDefaultConfigContent()
    {
        // Act & Assert
        ConfigInitializer.BuildConfigContent(new Config())
            .Should().Be(ConfigInitializer.BuildDefaultConfigContent());
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileIsUpToDate_ShouldNotModifyFile()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var original = ConfigInitializer.BuildDefaultConfigContent();
        File.WriteAllText(configPath, original);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        File.ReadAllText(configPath).Should().Be(original);

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelKeyMissing_ShouldAddItWithDefaultValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "showUser": false,
              "showHost": true
            }
            """;
        File.WriteAllText(configPath, contentMissingKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"compact\":");
        result.Should().Contain("\"multilinePrompt\":");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelKeyMissing_ShouldPreserveExistingCustomValues()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "showUser": false,
              "showHost": false
            }
            """;
        File.WriteAllText(configPath, contentMissingKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showUser\": false");
        result.Should().Contain("\"showHost\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenNestedKeyMissing_ShouldAddItWithDefaultValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingNestedKey = """
            {
              "cache": {
                "gitStatusTtl": 10
              }
            }
            """;
        File.WriteAllText(configPath, contentMissingNestedKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"repositoryTtl\":");
        result.Should().Contain("\"gitStatusTtl\": 10");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelBoolKeyMissing_ShouldWriteApplicationDefaultNotClrDefault()
    {
        // Arrange — partial config without the keys that have application-default=true.
        // CLR default for bool is false; application defaults for these five keys are true.
        var configPath = Path.GetTempFileName();
        var contentMissingBools = """
            {
              "compact": false
            }
            """;
        File.WriteAllText(configPath, contentMissingBools);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert — absent bool keys should be written with the application default (true), not CLR default (false)
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showUser\": true");
        result.Should().Contain("\"showHost\": true");
        result.Should().Contain("\"multilinePrompt\": true");
        result.Should().Contain("\"showCommandDuration\": true");
        result.Should().Contain("\"showStashInCompactMode\": true");

        // Existing explicit value must be preserved
        result.Should().Contain("\"compact\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenBoolKeyExplicitlyFalse_ShouldPreserveUserFalse()
    {
        // Arrange — user explicitly set showCommandDuration to false. Migration must not overwrite it.
        var configPath = Path.GetTempFileName();
        var contentWithExplicitFalse = """
            {
              "showCommandDuration": false
            }
            """;
        File.WriteAllText(configPath, contentWithExplicitFalse);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showCommandDuration\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenCommandDurationMinMsSet_ShouldPreserveUserValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentWithThreshold = """
            {
              "commandDurationMinMs": 5000
            }
            """;
        File.WriteAllText(configPath, contentWithThreshold);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"commandDurationMinMs\": 5000");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileIsUnparseable_ShouldNotThrowAndNotModifyFile()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        const string malformed = "{ this is not valid json }}}";
        File.WriteAllText(configPath, malformed);

        // Act
        var act = () => ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        act.Should().NotThrow();
        File.ReadAllText(configPath).Should().Be(malformed);

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenFileDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.jsonc");

        // Act
        var act = () => ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        act.Should().NotThrow();
    }


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
    public void BuildDefaultConfigContent_ShouldRenderCommandDurationMinMsAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"commandDurationMinMs\": null");
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
        content.Should().Contain("\"branchLabelOpen\": null");
        content.Should().Contain("\"branchLabelClose\": null");
        content.Should().Contain("\"branchOperationSeparator\": null");
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
    public void BuildDefaultConfigContent_ShouldRenderBranchLabelBracketDefaultGlyphs()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain($"null = default: {BranchLabelTokens.BranchLabelOpen}");
        content.Should().Contain($"null = default: {BranchLabelTokens.BranchLabelClose}");
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
