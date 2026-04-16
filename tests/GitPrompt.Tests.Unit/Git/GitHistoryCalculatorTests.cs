using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitHistoryCalculatorTests
{
    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_WhenInputVaries_ShouldQuoteOnlyWhenRequired(string value, string expected)
    {
        // Act
        var escapedValue = GitHistoryCalculator.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_WhenInputContainsBackslashesAndQuotes_ShouldEscapeCharactersInsideQuotedValue()
    {
        // Arrange
        const string value = "C:\\Program Files\\My \"App\"";

        // Act
        var escapedValue = GitHistoryCalculator.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }
}
