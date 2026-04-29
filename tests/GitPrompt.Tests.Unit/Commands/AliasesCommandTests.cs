using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class AliasesCommandTests
{
    [Fact]
    public void Run_WhenAliasesFileDoesNotExist_ShouldWriteInformativeErrorMessage()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.sh");
        var errorOutput = new StringWriter();

        // Act
        AliasesCommand.Run(nonExistentPath, errorOutput);

        // Assert
        var error = errorOutput.ToString();
        error.Should().Contain("git aliases not found at:");
        error.Should().Contain("gitprompt update aliases");
        error.Should().Contain(nonExistentPath);
    }
}
