using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Unit.Git;

public sealed class UtilitiesTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("123456", "123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("1234567890", "1234567")]
    public void ShortenCommitHash_WhenInputVaries_ShouldReturnShortForm(string objectId, string expectedShortHash)
    {
        // Act
        var shortHash = Utilities.ShortenCommitHash(objectId);

        // Assert
        shortHash.Should().Be(expectedShortHash);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_WhenInputVaries_ShouldQuoteOnlyWhenRequired(string argument, string expected)
    {
        // Act
        var escapedValue = Utilities.EscapeCommandLineArgument(argument);

        // Assert
        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_WhenInputContainsBackslashesAndQuotes_ShouldEscapeCharactersInsideQuotedValue()
    {
        // Arrange
        const string argument = "C:\\Program Files\\My \"App\"";

        // Act
        var escapedValue = Utilities.EscapeCommandLineArgument(argument);

        // Assert
        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }

    [Fact]
    public void EnumerateLines_WhenTextContainsMultipleLines_ShouldYieldEachLine()
    {
        // Arrange
        const string text = "line1\nline2\nline3";

        // Act
        var lines = Utilities.EnumerateLines(text).ToList();

        // Assert
        lines.Should().HaveCount(3);
        lines.Should().ContainInOrder("line1", "line2", "line3");
    }

    [Fact]
    public void EnumerateLines_WhenTextIsEmpty_ShouldYieldNoLines()
    {
        // Arrange
        const string text = "";

        // Act
        var lines = Utilities.EnumerateLines(text).ToList();

        // Assert
        lines.Should().BeEmpty();
    }

    [Fact]
    public void EnumerateLines_WhenTextContainsSingleLine_ShouldYieldOneLine()
    {
        // Arrange
        const string text = "single line";

        // Act
        var lines = Utilities.EnumerateLines(text).ToList();

        // Assert
        lines.Should().HaveCount(1);
        lines[0].Should().Be("single line");
    }
}
