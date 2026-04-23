using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class InitCommandTests
{
    [Fact]
    public void GenerateBashInit_ShouldNotIncludeMultiWordVerbsInTopLevelCompletions()
    {
        // Arrange
        var multiWordVerbs = CommandRegistry.VisibleCommands
            .Select(command => command.Verb)
            .Where(verb => verb.Contains(' '))
            .ToList();

        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        var topLevelCompletionLine = script
            .Split('\n')
            .Single(line => line.TrimStart().StartsWith("gitprompt|gitprompt.exe)"));

        foreach (var verb in multiWordVerbs)
        {
            topLevelCompletionLine.Should().NotContain(verb,
                because: $"multi-word verb '{verb}' must not appear in top-level completions");
        }
    }

    [Fact]
    public void GenerateBashInit_ShouldIncludeTopLevelVerbsInCompletions()
    {
        // Arrange
        var expectedVerbs = CommandRegistry.VisibleCommands
            .Select(command => command.Verb)
            .Where(verb => !verb.Contains(' '))
            .ToList();

        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        var topLevelCompletionLine = script
            .Split('\n')
            .Single(line => line.TrimStart().StartsWith("gitprompt|gitprompt.exe)"));

        foreach (var verb in expectedVerbs)
        {
            topLevelCompletionLine.Should().Contain(verb,
                because: $"top-level verb '{verb}' should appear in completions");
        }
    }

    [Fact]
    public void GenerateBashInit_ShouldDefinePromptSpFunction()
    {
        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().Contain("__gitprompt_prompt_sp()",
            because: "the script must define the __gitprompt_prompt_sp function for the partial-line indicator");
    }

    [Fact]
    public void GenerateBashInit_ShouldCallPromptSpInsideUpdatePs1()
    {
        // Arrange
        var lines = InitCommand.GenerateBashInit().Split('\n');

        // Act
        var updatePs1Start = Array.FindIndex(lines, l => l.Contains("_gitprompt_update_ps1()"));
        var updatePs1End   = Array.FindIndex(lines, updatePs1Start + 1, l => l.TrimStart().StartsWith("}"));

        // Assert
        updatePs1Start.Should().BeGreaterThan(-1, "the script must contain _gitprompt_update_ps1");
        var updatePs1Body = string.Join('\n', lines[updatePs1Start..updatePs1End]);
        updatePs1Body.Should().Contain("__gitprompt_prompt_sp",
            because: "_gitprompt_update_ps1 must call __gitprompt_prompt_sp so the partial-line check runs on every prompt render");
    }
}
