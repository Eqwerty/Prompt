namespace GitPrompt.Commands;

internal static class EditorResolver
{
    internal static string GetEditor()
    {
        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrEmpty(editor))
        {
            return editor;
        }

        var visual = Environment.GetEnvironmentVariable("VISUAL");
        if (!string.IsNullOrEmpty(visual))
        {
            return visual;
        }

        return "vim";
    }
}
