using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class HelpCommandTests
{
    public static TheoryData<string> VisibleCommandUsages => [..CommandRegistry.VisibleCommands.Select(command => command.Usage)];

    [Theory]
    [MemberData(nameof(VisibleCommandUsages))]
    public void PrintHelp_ShouldOutputEachVisibleCommandUsage(string usage)
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        output.ToString().Should().Contain(usage);
    }

    [Fact]
    public void PrintHelp_ShouldNotOutputHiddenCommands()
    {
        // Arrange
        var output = new StringWriter();
        var hiddenUsages = CommandRegistry.Commands
            .Where(command => command.IsHidden)
            .Select(command => command.Usage)
            .ToList();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var text = output.ToString();
        foreach (var usage in hiddenUsages)
        {
            text.Should().NotContain(usage);
        }
    }

    [Fact]
    public void PrintHelp_ShouldOutputAllVisibleDescriptions()
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var text = output.ToString();
        foreach (var command in CommandRegistry.VisibleCommands)
        {
            text.Should().Contain(command.Description);
        }
    }

    [Fact]
    public void PrintHelp_ShouldAlignDescriptionsToTheSameColumn()
    {
        // Arrange
        var output = new StringWriter();
        var expectedPadWidth = CommandRegistry.VisibleCommands.Max(command => command.Usage.Length) + 5;
        var expectedDescriptionColumn = 2 + expectedPadWidth;

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var commandLines = output.ToString()
            .Split('\n')
            .Where(line => line.StartsWith("  gitprompt"))
            .ToList();

        commandLines.Should().NotBeEmpty();
        commandLines.Should().OnlyContain(
            line => line.Length > expectedDescriptionColumn && !char.IsWhiteSpace(line[expectedDescriptionColumn]),
            $"all descriptions should start at column {expectedDescriptionColumn}");
    }
}
