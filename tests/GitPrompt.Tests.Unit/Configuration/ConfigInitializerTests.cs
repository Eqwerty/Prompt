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
            Context = new Config.ContextConfig { ShowUser = false, MaxPathDepth = 3 },
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
        // Arrange — config has a context group but is missing layout and commandDuration groups entirely
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "context": {
                "showUser": false
              }
            }
            """;
        File.WriteAllText(configPath, contentMissingKey);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"layout\":");
        result.Should().Contain("\"commandDuration\":");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenTopLevelKeyMissing_ShouldPreserveExistingCustomValues()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentMissingKey = """
            {
              "context": {
                "showUser": false,
                "showDomain": false,
                "showHost": false,
                "maxPathDepth": 0
              }
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
    public void MigrateConfigIfNeeded_WhenKeysMissing_ShouldWriteDefaultValuesForAbsentKeys()
    {
        // Arrange — partial config; context, layout, commandDuration, showStash are all absent
        var configPath = Path.GetTempFileName();
        var contentWithOnlyCompact = """
            {
              "compact": false
            }
            """;
        File.WriteAllText(configPath, contentWithOnlyCompact);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert — absent keys are written with their actual default values
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"showUser\": true");
        result.Should().Contain("\"showHost\": true");
        result.Should().Contain("\"showPath\": true");
        result.Should().Contain("\"multiline\": true");
        result.Should().Contain("\"show\": true");
        result.Should().Contain("\"showStash\": true");
        result.Should().Contain("\"startOfLine\": true");

        // Existing explicit value must be preserved
        result.Should().Contain("\"compact\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenBoolKeyExplicitlyFalse_ShouldPreserveUserFalse()
    {
        // Arrange — user explicitly set commandDuration.show to false.
        var configPath = Path.GetTempFileName();
        var contentWithExplicitFalse = """
            {
              "commandDuration": {
                "show": false,
                "minMs": null
              }
            }
            """;
        File.WriteAllText(configPath, contentWithExplicitFalse);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"show\": false");

        File.Delete(configPath);
    }

    [Fact]
    public void MigrateConfigIfNeeded_WhenCommandDurationMinMsSet_ShouldPreserveUserValue()
    {
        // Arrange
        var configPath = Path.GetTempFileName();
        var contentWithThreshold = """
            {
              "commandDuration": {
                "show": true,
                "minMs": 5000
              }
            }
            """;
        File.WriteAllText(configPath, contentWithThreshold);

        // Act
        ConfigInitializer.MigrateConfigIfNeeded(configPath);

        // Assert
        var result = File.ReadAllText(configPath);
        result.Should().Contain("\"minMs\": 5000");

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
    public void BuildDefaultConfigContent_ShouldRenderCommandTimeoutMsAsDefault()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"commandTimeoutMs\": 2000");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderCommandDurationMinMsAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — null = always show
        content.Should().Contain("\"minMs\": null");
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
        ConfigInitializer.BuildDefaultConfigContent().Should().Contain("\"show\":");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderContextDefaultValues()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"showUser\": true");
        content.Should().Contain("\"showHost\": true");
        content.Should().Contain("\"showDomain\": false");
        content.Should().Contain("\"showPath\": true");
        content.Should().Contain("\"maxPathDepth\": 0");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderLayoutDefaultValues()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert
        content.Should().Contain("\"multiline\": true");
        content.Should().Contain("\"newlineBefore\": false");
        content.Should().Contain("\"startOfLine\": true");
        content.Should().Contain("\"symbol\": null");  // null = automatic
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderPromptSymbolAsNull()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — null = automatic shell symbol
        content.Should().Contain("\"symbol\": null");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllIconValuesAsDefaults()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — icons render as actual glyph values from PromptIcons/BranchLabelTokens
        content.Should().Contain($"\"ahead\": \"{PromptIcons.IconAhead}\"");
        content.Should().Contain($"\"behind\": \"{PromptIcons.IconBehind}\"");
        content.Should().Contain($"\"added\": \"{PromptIcons.IconAdded}\"");
        content.Should().Contain($"\"modified\": \"{PromptIcons.IconModified}\"");
        content.Should().Contain($"\"renamed\": \"{PromptIcons.IconRenamed}\"");
        content.Should().Contain($"\"deleted\": \"{PromptIcons.IconDeleted}\"");
        content.Should().Contain($"\"untracked\": \"{PromptIcons.IconUntracked}\"");
        content.Should().Contain($"\"conflicts\": \"{PromptIcons.IconConflicts}\"");
        content.Should().Contain($"\"stash\": \"{PromptIcons.IconStash}\"");
        content.Should().Contain($"\"noUpstreamMarker\": \"{BranchLabelTokens.NoUpstreamBranchMarker}\"");
        content.Should().Contain($"\"detachedHeadMarker\": \"{BranchLabelTokens.DetachedHeadBranchMarker}\"");
        content.Should().Contain($"\"branchLabelOpen\": \"{BranchLabelTokens.BranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelClose\": \"{BranchLabelTokens.BranchLabelClose}\"");
        content.Should().Contain($"\"branchOperationSeparator\": \"{BranchLabelTokens.BranchOperationSeparator}\"");
        content.Should().Contain($"\"branchLabelOpenNormal\": \"{BranchLabelTokens.NormalBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseNormal\": \"{BranchLabelTokens.NormalBranchLabelClose}\"");
        content.Should().Contain($"\"branchLabelOpenNoUpstream\": \"{BranchLabelTokens.NoUpstreamBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseNoUpstream\": \"{BranchLabelTokens.NoUpstreamBranchLabelClose}\"");
        content.Should().Contain($"\"branchLabelOpenDetached\": \"{BranchLabelTokens.DetachedBranchLabelOpen}\"");
        content.Should().Contain($"\"branchLabelCloseDetached\": \"{BranchLabelTokens.DetachedBranchLabelClose}\"");
    }

    [Fact]
    public void BuildDefaultConfigContent_ShouldRenderAllColorValuesAsDefaults()
    {
        // Act
        var content = ConfigInitializer.BuildDefaultConfigContent();

        // Assert — color slots render as actual ANSI color codes
        content.Should().Contain("\"user\": \"[32m\"");
        content.Should().Contain("\"host\": \"[95m\"");
        content.Should().Contain("\"path\": \"[38;5;172m\"");
        content.Should().Contain("\"branch\": \"[1;36m\"");
        content.Should().Contain("\"branchDetached\": \"[0;33m\"");
        content.Should().Contain("\"promptSymbol\": \"[37m\"");
    }
}
