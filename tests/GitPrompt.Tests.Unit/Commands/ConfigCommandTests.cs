using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class ConfigCommandTests
{
    [Fact]
    public void GetConfigFilePath_WhenCalled_ShouldEndWithConfigJson()
    {
        // Act
        var path = ConfigCommand.GetConfigFilePath();

        // Assert
        path.Should().EndWith("config.json");
    }

    [Fact]
    public void GetConfigFilePath_WhenCalled_ShouldContainGitpromptDirectory()
    {
        // Act
        var path = ConfigCommand.GetConfigFilePath();

        // Assert
        path.ToLowerInvariant().Should().Contain("gitprompt");
    }

    [Fact]
    public void GetEditor_WhenEditorEnvVarIsSet_ShouldReturnIt()
    {
        // Act
        var editor = ConfigCommand.GetEditor("emacs", null);

        // Assert
        editor.Should().Be("emacs");
    }

    [Fact]
    public void GetEditor_WhenEditorIsNotSetButVisualIs_ShouldReturnVisual()
    {
        // Act
        var editor = ConfigCommand.GetEditor(null, "code");

        // Assert
        editor.Should().Be("code");
    }

    [Fact]
    public void GetEditor_WhenBothEditorAndVisualAreSet_ShouldPreferEditor()
    {
        // Act
        var editor = ConfigCommand.GetEditor("vim", "code");

        // Assert
        editor.Should().Be("vim");
    }

    [Fact]
    public void GetEditor_WhenNeitherEditorNorVisualIsSet_ShouldReturnVi()
    {
        // Act
        var editor = ConfigCommand.GetEditor(null, null);

        // Assert
        editor.Should().Be("vi");
    }
}
