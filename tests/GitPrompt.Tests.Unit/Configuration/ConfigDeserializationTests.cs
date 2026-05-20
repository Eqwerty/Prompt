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
        config!.Cache!.GitStatusTtl.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsNonZero_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 10, "repositoryTtl": 60}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache!.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsAbsent_ShouldReturnDefaultFiveSeconds()
    {
        // Arrange
        var json = """{"cache": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache!.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deserialize_WhenRepositoryTtlIsZero_ShouldReturnTimeSpanZero()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 5, "repositoryTtl": 0}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache!.RepositoryTtl.Should().Be(TimeSpan.Zero);
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
        config!.Cache!.GitStatusTtl.Should().Be(TimeSpan.Zero);
        config.Cache!.RepositoryTtl.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Deserialize_WhenGitStatusTtlIsFractional_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var json = """{"cache": {"gitStatusTtl": 2.5, "repositoryTtl": 60}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Cache!.GitStatusTtl.Should().Be(TimeSpan.FromSeconds(2.5));
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
    public void CommandDuration_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration.Should().BeNull();
    }

    [Fact]
    public void CommandDuration_Show_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"commandDuration": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.Show.Should().BeNull();
    }

    [Fact]
    public void CommandDuration_Show_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"commandDuration": {"show": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.Show.Should().BeTrue();
    }

    [Fact]
    public void Context_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context.Should().BeNull();
    }

    [Fact]
    public void Context_ShowUser_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"context": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowUser.Should().BeNull();
    }

    [Fact]
    public void Context_ShowUser_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"context": {"showUser": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowUser.Should().BeTrue();
    }

    [Fact]
    public void Context_ShowUser_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"context": {"showUser": false}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowUser.Should().BeFalse();
    }

    [Fact]
    public void Context_ShowHost_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"context": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowHost.Should().BeNull();
    }

    [Fact]
    public void Context_ShowHost_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"context": {"showHost": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowHost.Should().BeTrue();
    }

    [Fact]
    public void Context_ShowHost_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"context": {"showHost": false}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowHost.Should().BeFalse();
    }

    [Fact]
    public void Context_ShowPath_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"context": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowPath.Should().BeNull();
    }

    [Fact]
    public void Context_ShowPath_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"context": {"showPath": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowPath.Should().BeTrue();
    }

    [Fact]
    public void Context_ShowPath_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"context": {"showPath": false}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.ShowPath.Should().BeFalse();
    }

    [Fact]
    public void Context_MaxPathDepth_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"context": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.MaxPathDepth.Should().BeNull();
    }

    [Fact]
    public void Context_MaxPathDepth_WhenExplicitlySet_ShouldReturnValue()
    {
        // Arrange
        var json = """{"context": {"maxPathDepth": 3}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Context!.MaxPathDepth.Should().Be(3);
    }

    [Fact]
    public void Layout_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout.Should().BeNull();
    }

    [Fact]
    public void Layout_Multiline_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"layout": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Multiline.Should().BeNull();
    }

    [Fact]
    public void Layout_Multiline_WhenExplicitlyFalse_ShouldReturnFalse()
    {
        // Arrange
        var json = """{"layout": {"multiline": false}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Multiline.Should().BeFalse();
    }

    [Fact]
    public void Layout_Multiline_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"layout": {"multiline": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Multiline.Should().BeTrue();
    }

    [Fact]
    public void Layout_NewlineBefore_WhenPresentButEmpty_ShouldBeNull()
    {
        // Arrange
        var json = """{"layout": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.NewlineBefore.Should().BeNull();
    }

    [Fact]
    public void Layout_NewlineBefore_WhenExplicitlyTrue_ShouldReturnTrue()
    {
        // Arrange
        var json = """{"layout": {"newlineBefore": true}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.NewlineBefore.Should().BeTrue();
    }

    [Fact]
    public void Layout_Symbol_WhenAbsentFromGroup_ShouldDefaultToNull()
    {
        // Arrange
        var json = """{"layout": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Symbol.Should().BeNull();
    }

    [Fact]
    public void Layout_Symbol_WhenExplicitlySet_ShouldReturnValue()
    {
        // Arrange
        var json = """{"layout": {"symbol": "❯"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Symbol.Should().Be("❯");
    }

    [Fact]
    public void Layout_Symbol_WhenSetToEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var json = """{"layout": {"symbol": ""}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Layout!.Symbol.Should().BeEmpty();
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
        config.Icons!.Behind.Should().BeNull();
        config.Icons!.Added.Should().BeNull();
        config.Icons!.Modified.Should().BeNull();
        config.Icons!.Renamed.Should().BeNull();
        config.Icons!.Deleted.Should().BeNull();
        config.Icons!.Untracked.Should().BeNull();
        config.Icons!.Conflicts.Should().BeNull();
        config.Icons!.Stash.Should().BeNull();
        config.Icons!.NoUpstreamMarker.Should().BeNull();
    }

    [Fact]
    public void Icons_WhenExplicitlySet_ShouldReturnValues()
    {
        // Arrange
        var json = """{"icons": {"ahead": "⬆", "behind": "⬇", "stash": "S"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons!.Ahead.Should().Be("⬆");
        config.Icons!.Behind.Should().Be("⬇");
        config.Icons!.Stash.Should().Be("S");
        config.Icons!.Added.Should().BeNull();
    }

    [Fact]
    public void NoUpstreamMarker_WhenAbsent_ShouldBeNull()
    {
        // Arrange
        var json = """{"icons": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons!.NoUpstreamMarker.Should().BeNull();
    }

    [Fact]
    public void NoUpstreamMarker_WhenExplicitlySet_ShouldReturnValue()
    {
        // Arrange
        var json = """{"icons": {"noUpstreamMarker": "!"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Icons!.NoUpstreamMarker.Should().Be("!");
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
        config.Colors!.Host.Should().BeNull();
        config.Colors!.Path.Should().BeNull();
        config.Colors!.CommandDuration.Should().BeNull();
        config.Colors!.Branch.Should().BeNull();
        config.Colors!.BranchNoUpstream.Should().BeNull();
        config.Colors!.Ahead.Should().BeNull();
        config.Colors!.Behind.Should().BeNull();
        config.Colors!.Staged.Should().BeNull();
        config.Colors!.Unstaged.Should().BeNull();
        config.Colors!.Untracked.Should().BeNull();
        config.Colors!.Stash.Should().BeNull();
        config.Colors!.Conflict.Should().BeNull();
        config.Colors!.MissingPath.Should().BeNull();
        config.Colors!.Timeout.Should().BeNull();
        config.Colors!.PromptSymbol.Should().BeNull();
    }

    [Fact]
    public void Colors_WhenExplicitlySet_ShouldReturnValues()
    {
        // Arrange
        var json = """{"colors": {"user": "#FF0000", "branch": "#00FF00", "timeout": "#0000FF"}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.Colors!.User.Should().Be("#FF0000");
        config.Colors!.Branch.Should().Be("#00FF00");
        config.Colors!.Timeout.Should().Be("#0000FF");
        config.Colors!.Host.Should().BeNull();
    }

    [Fact]
    public void CommandDuration_MinMs_WhenAbsentFromGroup_ShouldBeNull()
    {
        // Arrange
        var json = """{"commandDuration": {}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.MinMs.Should().BeNull();
    }

    [Fact]
    public void CommandDuration_MinMs_WhenPresentAsInteger_ShouldDeserialize()
    {
        // Arrange
        var json = """{"commandDuration": {"minMs": 5000}}""";

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.MinMs.Should().Be(5000.0);
    }

    [Fact]
    public void CommandDuration_MinMs_WhenPresentWithInlineComment_ShouldDeserialize()
    {
        // Arrange - matches the real config file format with JSONC inline comment
        var json = """
            {
              "commandDuration": {
                "minMs": 5000,  // minimum duration in ms before showing command duration (null = always show)
                "show": true
              }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.MinMs.Should().Be(5000.0);
        config.CommandDuration!.Show.Should().BeTrue();
    }

    [Fact]
    public void CommandDuration_Show_WhenFalseWithInlineComment_ShouldDeserializeToFalse()
    {
        // Arrange - matches the real config file format with JSONC inline comment
        var json = """
            {
              "commandDuration": {
                "show": false  // show last command duration in the prompt (true/false)
              }
            }
            """;

        // Act
        var config = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.Config);

        // Assert
        config!.CommandDuration!.Show.Should().BeFalse();
    }
}

