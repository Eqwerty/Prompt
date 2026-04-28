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
}
