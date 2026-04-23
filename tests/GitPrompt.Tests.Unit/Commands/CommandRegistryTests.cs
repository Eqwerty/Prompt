using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class CommandRegistryTests
{
    [Fact]
    public void Commands_ShouldNotBeEmpty()
    {
        // Act & Assert
        CommandRegistry.Commands.Should().NotBeEmpty();
    }

    [Fact]
    public void Commands_ShouldHaveNonEmptyDescriptionForEveryEntry()
    {
        // Act & Assert
        CommandRegistry.Commands.Should().OnlyContain(command => !string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public void Commands_ShouldNotHaveDuplicateVerbs()
    {
        // Arrange
        var allVerbs = CommandRegistry.Commands.Select(command => command.Verb);

        // Act & Assert
        allVerbs.Should().OnlyHaveUniqueItems();
    }
}
