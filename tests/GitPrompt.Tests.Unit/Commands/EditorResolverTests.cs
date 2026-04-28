using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class EditorResolverTests
{
    [Fact]
    public void GetEditor_WhenEditorIsSet_ShouldReturnEditor()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EDITOR", "nano");
        Environment.SetEnvironmentVariable("VISUAL", null);

        try
        {
            // Act
            var editor = EditorResolver.GetEditor();

            // Assert
            editor.Should().Be("nano");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", null);
        }
    }

    [Fact]
    public void GetEditor_WhenVisualIsSetAndEditorIsNot_ShouldReturnVisual()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EDITOR", null);
        Environment.SetEnvironmentVariable("VISUAL", "code");

        try
        {
            // Act
            var editor = EditorResolver.GetEditor();

            // Assert
            editor.Should().Be("code");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISUAL", null);
        }
    }

    [Fact]
    public void GetEditor_WhenNeitherEditorNorVisualIsSet_ShouldReturnVim()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EDITOR", null);
        Environment.SetEnvironmentVariable("VISUAL", null);

        // Act
        var editor = EditorResolver.GetEditor();

        // Assert
        editor.Should().Be("vim");
    }

    [Fact]
    public void GetEditor_WhenBothEditorAndVisualAreSet_ShouldPreferEditor()
    {
        // Arrange
        Environment.SetEnvironmentVariable("EDITOR", "nano");
        Environment.SetEnvironmentVariable("VISUAL", "code");

        try
        {
            // Act
            var editor = EditorResolver.GetEditor();

            // Assert
            editor.Should().Be("nano");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", null);
            Environment.SetEnvironmentVariable("VISUAL", null);
        }
    }
}
