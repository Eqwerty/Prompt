using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class CommandRegistryTests
{
    [Fact]
    public void All_ShouldNotBeEmpty()
    {
        // Act & Assert
        CommandRegistry.All.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("gitprompt init bash")]
    [InlineData("gitprompt config")]
    [InlineData("gitprompt update")]
    [InlineData("gitprompt uninstall")]
    [InlineData("gitprompt --help")]
    public void All_WhenCommandIsHandledByArgumentProcessor_ShouldHaveMatchingEntry(string usage)
    {
        // Act & Assert
        CommandRegistry.All.Should().Contain(c => c.Usage == usage);
    }

    [Fact]
    public void All_ShouldHaveNonEmptyDescriptionForEveryEntry()
    {
        // Act & Assert
        CommandRegistry.All.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Description));
    }
}

public sealed class HelpCommandTests
{
    [Theory]
    [InlineData("gitprompt init bash")]
    [InlineData("gitprompt config")]
    [InlineData("gitprompt update")]
    [InlineData("gitprompt uninstall")]
    [InlineData("gitprompt --help")]
    public void PrintHelp_ShouldOutputEachCommandUsage(string usage)
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        HelpCommand.PrintHelp();

        // Assert
        output.ToString().Should().Contain(usage);
    }

    [Fact]
    public void PrintHelp_ShouldOutputAllDescriptions()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        HelpCommand.PrintHelp();

        // Assert
        var text = output.ToString();
        foreach (var command in CommandRegistry.All)
            text.Should().Contain(command.Description);
    }

    [Fact]
    public void PrintHelp_ShouldAlignDescriptionsToTheSameColumn()
    {
        // Arrange
        var output = new StringWriter();
        Console.SetOut(output);
        var expectedPadWidth = CommandRegistry.All.Max(c => c.Usage.Length) + 5;
        var expectedDescriptionColumn = 2 + expectedPadWidth; // 2 = leading indent

        // Act
        HelpCommand.PrintHelp();

        // Assert
        var commandLines = output.ToString()
            .Split('\n')
            .Where(l => l.StartsWith("  gitprompt"))
            .ToList();

        commandLines.Should().NotBeEmpty();
        commandLines.Should().OnlyContain(
            l => l.Length > expectedDescriptionColumn && !char.IsWhiteSpace(l[expectedDescriptionColumn]),
            $"all descriptions should start at column {expectedDescriptionColumn}");
    }
}
