using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public class ConfigCommandTests
{
    [Fact]
    public void GetConfigFilePath_EndsWithConfigJson()
    {
        var path = ConfigCommand.GetConfigFilePath();
        Assert.EndsWith("config.json", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetConfigFilePath_ContainsGitpromptDirectory()
    {
        var path = ConfigCommand.GetConfigFilePath();
        Assert.Contains("gitprompt", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEditor_ReturnsEditorEnvVar_WhenSet()
    {
        var editor = ConfigCommand.GetEditor("emacs", null);
        Assert.Equal("emacs", editor);
    }

    [Fact]
    public void GetEditor_FallsBackToVisual_WhenEditorNotSet()
    {
        var editor = ConfigCommand.GetEditor(null, "code");
        Assert.Equal("code", editor);
    }

    [Fact]
    public void GetEditor_PrefersEditor_OverVisual()
    {
        var editor = ConfigCommand.GetEditor("vim", "code");
        Assert.Equal("vim", editor);
    }

    [Fact]
    public void GetEditor_FallsBackToPlatformDefault_WhenNeitherSet()
    {
        var editor = ConfigCommand.GetEditor(null, null);
        Assert.True(editor is "vi" or "notepad.exe",
            $"Expected 'vi' or 'notepad.exe', got '{editor}'");
    }
}
