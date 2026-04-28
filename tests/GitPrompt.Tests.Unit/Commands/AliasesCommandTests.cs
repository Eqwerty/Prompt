using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class AliasesCommandTests
{
    [Fact]
    public void Run_WhenAliasesFileDoesNotExist_ShouldWriteNotFoundError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.Run(nonExistentPath, errorOutput);

        // Assert
        errorOutput.ToString().Should().Contain("git aliases not found at:");
    }

    [Fact]
    public void Run_WhenAliasesFileDoesNotExist_ShouldSuggestUpdateAliasesCommand()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.Run(nonExistentPath, errorOutput);

        // Assert
        errorOutput.ToString().Should().Contain("gitprompt update aliases");
    }

    [Fact]
    public void Run_WhenAliasesFileDoesNotExist_ShouldIncludePathInErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.Run(nonExistentPath, errorOutput);

        // Assert
        errorOutput.ToString().Should().Contain(nonExistentPath);
    }
}
