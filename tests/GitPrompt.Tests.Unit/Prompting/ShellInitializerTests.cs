using FluentAssertions;
using GitPrompt.Prompting;

namespace GitPrompt.Tests.Unit.Prompting;

public sealed class ShellInitializerTests
{
    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldContainPromptCommand()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        script.Should().Contain("PROMPT_COMMAND");
    }

    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldContainDebugTrap()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        script.Should().Contain("trap '__gitprompt_debug_trap' DEBUG");
    }

    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldContainInvalidateCacheCall()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        script.Should().Contain("--invalidate-status-cache");
    }

    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldResolveBinaryFromPath()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        script.Should().Contain("command -v gitprompt");
    }

    [Fact]
    public void GenerateBashInit_WhenFallbackPs1IsProvided_ShouldEmbedItInScript()
    {
        // Arrange
        const string fallback = "\\w >";

        // Act
        var script = ShellInitializer.GenerateBashInit(fallback);

        // Assert
        script.Should().Contain($"PS1='{fallback}'");
    }

    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldNotContainHardcodedAbsolutePath()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        // The script must resolve the binary from PATH at shell startup, not bake in an absolute path.
        script.Should().NotContain("/home/");
        script.Should().NotContain("/Users/");
        script.Should().NotContain("C:\\");
    }

    [Fact]
    public void GenerateBashInit_WhenCalled_ShouldRegisterTabCompletion()
    {
        // Act
        var script = ShellInitializer.GenerateBashInit(@"\w \$ ");

        // Assert
        script.Should().Contain("complete -F _gitprompt_complete gitprompt");
        script.Should().Contain("init config update uninstall --help");
    }
}
