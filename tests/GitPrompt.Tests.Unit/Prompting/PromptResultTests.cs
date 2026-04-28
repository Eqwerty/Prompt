using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Tests.Unit.Prompting;

[Collection(ConfigIsolationCollection.Name)]
public sealed class PromptResultTests
{
    private static PromptResult MakeResult(string context = "ctx", string symbol = "$") =>
        new(context, string.Empty, string.Empty, symbol,
            TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

    [Fact]
    public void Output_WhenMultilinePromptIsTrue_ShouldPutSymbolOnNewLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = true });
        var result = MakeResult();

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"ctx\n{ColorPromptSymbol}${ColorReset} ");
    }

    [Fact]
    public void Output_WhenMultilinePromptIsFalse_ShouldPutSymbolOnSameLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = false });
        var result = MakeResult();

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"ctx {ColorPromptSymbol}${ColorReset} ");
    }

    [Fact]
    public void Output_WhenMultilinePromptIsFalseAndGitStatusPresent_ShouldKeepAllOnOneLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = false });
        var result = new PromptResult("ctx", string.Empty, "(main)", "$",
            TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"ctx (main) {ColorPromptSymbol}${ColorReset} ");
    }

    [Fact]
    public void Output_WhenNewlineBeforePromptIsTrue_ShouldPrependBlankLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = true, NewlineBeforePrompt = true });
        var result = MakeResult();

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"\nctx\n{ColorPromptSymbol}${ColorReset} ");
    }

    [Fact]
    public void Output_WhenNewlineBeforePromptIsFalse_ShouldNotPrependBlankLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = true, NewlineBeforePrompt = false });
        var result = MakeResult();

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"ctx\n{ColorPromptSymbol}${ColorReset} ");
    }

    [Fact]
    public void Output_WhenNewlineBeforePromptIsTrueAndMultilinePromptIsFalse_ShouldPrependBlankLineToSingleLine()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { MultilinePrompt = false, NewlineBeforePrompt = true });
        var result = MakeResult();

        // Act
        var output = result.Output;

        // Assert
        output.Should().Be($"\nctx {ColorPromptSymbol}${ColorReset} ");
    }
}
