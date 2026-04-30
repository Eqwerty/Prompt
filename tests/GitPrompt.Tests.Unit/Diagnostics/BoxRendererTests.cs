using FluentAssertions;
using GitPrompt.Constants;
using GitPrompt.Terminal;

namespace GitPrompt.Tests.Unit.Diagnostics;

public sealed class BoxRendererTests
{
    [Fact]
    public void Render_WhenCalled_ShouldEmbedTitleInTopBorder()
    {
        // Arrange
        var lines = new List<string?> { "  content line" };

        // Act
        var result = BoxRenderer.Render("My Title", lines, string.Empty);

        // Assert
        result.Should().Contain("My Title");
        result.Should().Contain("╭");
        result.Should().Contain("╮");
    }

    [Fact]
    public void Render_WhenCalled_ShouldWrapContentWithVerticalBars()
    {
        // Arrange
        var lines = new List<string?> { "  hello world" };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert
        result.Should().Contain("│");
        result.Should().Contain("hello world");
    }

    [Fact]
    public void Render_WhenNullLineProvided_ShouldRenderSectionSeparator()
    {
        // Arrange
        var lines = new List<string?> { "  first", null, "  second" };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert
        result.Should().Contain("├");
        result.Should().Contain("┤");
    }

    [Fact]
    public void Render_WhenCalled_ShouldHaveBottomBorder()
    {
        // Arrange
        var lines = new List<string?> { "  content" };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert
        result.Should().Contain("╰");
        result.Should().Contain("╯");
    }

    [Fact]
    public void Render_WhenAllRowsSameWidth_ShouldProduceConsistentBoxWidth()
    {
        // Arrange
        var lines = new List<string?> { "  short", null, "  also short" };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert — all lines in the result (stripping newlines) should have consistent total width
        var resultLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var distinctWidths = resultLines.Select(l => l.Length).Distinct().ToList();
        distinctWidths.Should().HaveCount(1, "all box lines should have the same total width");
    }

    [Fact]
    public void Render_WhenLongLinePresent_ShouldExpandBoxToFit()
    {
        // Arrange
        var longLine = "  " + new string('x', 80);
        var lines = new List<string?> { "  short", longLine };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert
        result.Should().Contain(longLine);
        var resultLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        resultLines.All(l => l.Length >= longLine.Length).Should().BeTrue();
    }

    [Fact]
    public void Render_WhenLongTitlePresent_ShouldExpandBoxToFitTitle()
    {
        // Arrange
        var longTitle = new string('T', 60);
        var lines = new List<string?> { "  short" };

        // Act
        var result = BoxRenderer.Render(longTitle, lines, string.Empty);

        // Assert
        result.Should().Contain(longTitle);
        var topBorderLine = result.Split('\n')[0];
        topBorderLine.Length.Should().BeGreaterThan(longTitle.Length);
    }

    [Fact]
    public void Render_WhenBorderColorProvided_ShouldIncludeAnsiEscapeAndResetSequences()
    {
        // Arrange
        var lines = new List<string?> { "  content" };

        // Act
        var result = BoxRenderer.Render("Title", lines, AnsiColors.LightGray);

        // Assert
        result.Should().Contain(AnsiColorConverter.ToAnsi(AnsiColors.LightGray));
        result.Should().Contain(AnsiColors.Reset);
    }

    [Fact]
    public void Render_WhenMultipleNullLines_ShouldRenderMultipleSeparators()
    {
        // Arrange
        var lines = new List<string?> { "  a", null, "  b", null, "  c" };

        // Act
        var result = BoxRenderer.Render("Title", lines, string.Empty);

        // Assert — count occurrences of ├
        result.Count(c => c == '├').Should().Be(2);
    }
}
