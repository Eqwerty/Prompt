using System.Text.Json;
using FluentAssertions;
using GitPrompt.Configuration;

namespace GitPrompt.Tests.Unit.Configuration;

public sealed class ConfigDeserializationTests
{
    [Fact]
    public void Deserialize_WhenGitStatusTtlIsZero_ShouldReturnTimeSpanZero()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 0, "repositoryTtl": 60}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.GitStatusTtl.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsNonZero_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 10, "repositoryTtl": 60}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsAbsent_ShouldReturnDefaultFiveSeconds()
    {
        // Arrange
        var json = """{"cache": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deserialize_WhenRepositoryTtlIsZero_ShouldReturnTimeSpanZero()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 5, "repositoryTtl": 0}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.RepositoryTtl.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Deserialize_WhenJsonHasComments_ShouldParseSuccessfully()
    {
        // Arrange — matches the real config file format
        var json = """
                   {
                     "cache": {
                       "gitStatusTtl": 0,   // git status cache TTL in seconds (0 = disabled)
                       "repositoryTtl": 60  // repository location cache TTL in seconds (0 = disabled)
                     }
                   }
                   """;

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.GitStatusTtl.Should().Be(TimeSpan.Zero);
        config.Cache.RepositoryTtl.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsFractional_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 2.5, "repositoryTtl": 60}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(2.5));
    }

    [Fact]
    public void CommandTimeout_WhenCommandTimeoutMsIsAbsent_ShouldReturnDefault2000Ms()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandTimeout.Should().Be(TimeSpan.FromMilliseconds(2000));
    }

    [Fact]
    public void CommandTimeout_WhenCommandTimeoutMsIsExplicitValue_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var json = """{"commandTimeoutMs": 500}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandTimeout.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void CommandTimeout_WhenCommandTimeoutMsIsZero_ShouldReturnNull()
    {
        // Arrange
        var json = """{"commandTimeoutMs": 0}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void CommandTimeout_WhenCommandTimeoutMsIsNegative_ShouldReturnNull()
    {
        // Arrange
        var json = """{"commandTimeoutMs": -1}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void ShowCommandDuration_WhenAbsent_ShouldDefaultToFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowCommandDuration.Should().BeFalse();
    }

    [Fact]
    public void ShowCommandDuration_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"showCommandDuration": true}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowCommandDuration.Should().BeTrue();
    }

    [Fact]
    public void ShowUser_WhenAbsent_ShouldDefaultToFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowUser.Should().BeFalse();
    }

    [Fact]
    public void ShowUser_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"showUser": true}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowUser.Should().BeTrue();
    }

    [Fact]
    public void ShowUser_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"showUser": false}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowUser.Should().BeFalse();
    }

    [Fact]
    public void ShowHost_WhenAbsent_ShouldDefaultToFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowHost.Should().BeFalse();
    }

    [Fact]
    public void ShowHost_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"showHost": true}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowHost.Should().BeTrue();
    }

    [Fact]
    public void ShowHost_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"showHost": false}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.ShowHost.Should().BeFalse();
    }

    [Fact]
    public void MaxPathDepth_WhenAbsent_ShouldDefaultToZero()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.MaxPathDepth.Should().Be(0);
    }

    [Fact]
    public void MaxPathDepth_WhenExplicitlySet_ShouldReturnValue()
    {
        // Arrange
        var json = """{"maxPathDepth": 3}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.MaxPathDepth.Should().Be(3);
    }

    [Fact]
    public void MultilinePrompt_WhenAbsent_ShouldDefaultToFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.MultilinePrompt.Should().BeFalse();
    }

    [Fact]
    public void MultilinePrompt_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"multilinePrompt": false}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.MultilinePrompt.Should().BeFalse();
    }

    [Fact]
    public void MultilinePrompt_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"multilinePrompt": true}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.MultilinePrompt.Should().BeTrue();
    }

    [Fact]
    public void NewlineBeforePrompt_WhenAbsent_ShouldDefaultToFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.NewlineBeforePrompt.Should().BeFalse();
    }

    [Fact]
    public void NewlineBeforePrompt_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"newlineBeforePrompt": true}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.NewlineBeforePrompt.Should().BeTrue();
    }

    [Fact]
    public void PromptSymbol_WhenAbsent_ShouldDefaultToNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.PromptSymbol.Should().BeNull();
    }

    [Fact]
    public void PromptSymbol_WhenExplicitlySet_ShouldReturnValue()
    {
        // Arrange
        var json = """{"promptSymbol": "❯"}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.PromptSymbol.Should().Be("❯");
    }

    [Fact]
    public void PromptSymbol_WhenSetToEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var json = """{"promptSymbol": ""}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.PromptSymbol.Should().BeEmpty();
    }

    [Fact]
    public void Icons_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons.Should().BeNull();
    }

    [Fact]
    public void Icons_WhenPresentButEmpty_ShouldHaveAllNullIconValues()
    {
        // Arrange
        var json = """{"icons": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons.Should().NotBeNull();
        config.Icons!.Ahead.Should().BeNull();
        config.Icons.Behind.Should().BeNull();
        config.Icons.Added.Should().BeNull();
        config.Icons.Modified.Should().BeNull();
        config.Icons.Renamed.Should().BeNull();
        config.Icons.Deleted.Should().BeNull();
        config.Icons.Untracked.Should().BeNull();
        config.Icons.Conflicts.Should().BeNull();
        config.Icons.Stash.Should().BeNull();
    }

    [Fact]
    public void Icons_WhenExplicitlySet_ShouldReturnValues()
    {
        // Arrange
        var json = """{"icons": {"ahead": "⬆", "behind": "⬇", "stash": "S"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons.Ahead.Should().Be("⬆");
        config.Icons.Behind.Should().Be("⬇");
        config.Icons.Stash.Should().Be("S");
        config.Icons.Added.Should().BeNull();
    }

    [Fact]
    public void Colors_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Colors.Should().BeNull();
    }

    [Fact]
    public void Colors_WhenPresentButEmpty_ShouldHaveAllNullValues()
    {
        // Arrange
        var json = """{"colors": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Colors.Should().NotBeNull();
        config.Colors!.User.Should().BeNull();
        config.Colors.Host.Should().BeNull();
        config.Colors.Path.Should().BeNull();
        config.Colors.CommandDuration.Should().BeNull();
        config.Colors.Branch.Should().BeNull();
        config.Colors.BranchNoUpstream.Should().BeNull();
        config.Colors.Ahead.Should().BeNull();
        config.Colors.Behind.Should().BeNull();
        config.Colors.Staged.Should().BeNull();
        config.Colors.Unstaged.Should().BeNull();
        config.Colors.Untracked.Should().BeNull();
        config.Colors.Stash.Should().BeNull();
        config.Colors.Conflict.Should().BeNull();
        config.Colors.MissingPath.Should().BeNull();
        config.Colors.Timeout.Should().BeNull();
        config.Colors.PromptSymbol.Should().BeNull();
    }

    [Fact]
    public void Colors_WhenExplicitlySet_ShouldReturnValues()
    {
        // Arrange
        var json = """{"colors": {"user": "#FF0000", "branch": "#00FF00", "timeout": "#0000FF"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Colors.User.Should().Be("#FF0000");
        config.Colors.Branch.Should().Be("#00FF00");
        config.Colors.Timeout.Should().Be("#0000FF");
        config.Colors.Host.Should().BeNull();
    }
}
