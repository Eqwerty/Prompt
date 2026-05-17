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
            because: "_gitprompt_update_ps1 must reference __gitprompt_prompt_sp so the partial-line check can run on prompt render");
    }

    [Fact]
    public void GenerateBashInit_WhenPromptStartOfLineIsTrue_ShouldSetVariableTo1()
    {
        // Arrange — default config has Layout.StartOfLine = true

        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().Contain("_GITPROMPT_PROMPT_START_OF_LINE=1",
            because: "when promptStartOfLine is true the generated variable must be 1");
    }

    [Fact]
    public void GenerateBashInit_ShouldNotContainUnresolvedPromptStartOfLinePlaceholder()
    {
        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().NotContain("{{GITPROMPT_PROMPT_START_OF_LINE}}",
            because: "the placeholder must be replaced with a concrete value at init time");
    }

    [Fact]
    public void GenerateBashInit_ShouldGuardPromptCommandAgainstDuplication()
    {
        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().Contain("_gitprompt_update_ps1*",
            because: "the guard must check whether _gitprompt_update_ps1 is already in PROMPT_COMMAND");
        script.Should().Contain("PROMPT_COMMAND=\"_gitprompt_update_ps1",
            because: "the guard must prepend _gitprompt_update_ps1 to PROMPT_COMMAND when not already present");
    }

    [Fact]
    public void GenerateBashInit_ShouldGuardTimingVariableInitializationAgainstReset()
    {
        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().Contain("[ -z \"${__gitprompt_cmd_start_us+x}\" ]",
            because: "timing variables must only be initialized on first eval so re-sourcing does not " +
                     "clear __gitprompt_cmd_start_us while the DEBUG trap is active");
    }

    [Fact]
    public void GenerateBashInit_ShouldContainAliasesEnableDisableCompletion()
    {
        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        script.Should().Contain("aliases)",
            because: "the completion handler must have a case for 'aliases' to offer subcommand completions");
        script.Should().Contain("enable",
            because: "the aliases completion must include 'enable'");
        script.Should().Contain("disable",
            because: "the aliases completion must include 'disable'");
    }
}
