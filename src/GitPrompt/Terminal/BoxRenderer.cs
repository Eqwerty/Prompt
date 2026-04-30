using System.Text;
using GitPrompt.Constants;

namespace GitPrompt.Terminal;

/// <summary>
/// Renders a list of content lines inside a Unicode box with optional section separators.
/// Pass <see langword="null"/> as a line to insert a section separator (├──┤).
/// </summary>
internal static class BoxRenderer
{
    private const char TopLeft = '╭';
    private const char TopRight = '╮';
    private const char BottomLeft = '╰';
    private const char BottomRight = '╯';
    private const char Horizontal = '─';
    private const char Vertical = '│';
    private const char SeparatorLeft = '├';
    private const char SeparatorRight = '┤';

    /// <summary>
    /// Renders <paramref name="lines"/> inside a box.
    /// <see langword="null"/> entries produce section separator lines.
    /// </summary>
    internal static string Render(string title, IReadOnlyList<string?> lines, string borderColor)
    {
        var innerWidth = ComputeInnerWidth(title, lines);
        var ansiColor = string.IsNullOrEmpty(borderColor) ? string.Empty : AnsiColorConverter.ToAnsi(borderColor);
        var reset = string.IsNullOrEmpty(borderColor) ? string.Empty : AnsiColors.Reset;

        var sb = new StringBuilder();

        AppendTopBorder(sb, title, innerWidth, ansiColor, reset);

        foreach (var line in lines)
        {
            if (line is null)
            {
                AppendSeparator(sb, innerWidth, ansiColor, reset);
            }
            else
            {
                AppendContentLine(sb, line, innerWidth, ansiColor, reset);
            }
        }

        AppendBottomBorder(sb, innerWidth, ansiColor, reset);

        return sb.ToString();
    }

    private static int ComputeInnerWidth(string title, IReadOnlyList<string?> lines)
    {
        // Title needs "─ " prefix and " ─" suffix inside the border, so minimum = title.Length + 4
        var minForTitle = title.Length + 4;

        var maxLineLength = 0;
        foreach (var line in lines)
        {
            if (line is not null && line.Length > maxLineLength)
            {
                maxLineLength = line.Length;
            }
        }

        // Add 2 for the space padding on each side of the content ("│ content │")
        var minForContent = maxLineLength + 2;

        return Math.Max(minForTitle, minForContent);
    }

    private static void AppendTopBorder(StringBuilder sb, string title, int innerWidth, string borderColor, string reset)
    {
        // ╭─ Title ──────────╮
        var dashesBeforeTitle = 2;
        var totalDashesAfterTitle = innerWidth - title.Length - dashesBeforeTitle - 2; // -2 for space before and after title

        sb.Append(borderColor);
        sb.Append(TopLeft);
        sb.Append(Horizontal, dashesBeforeTitle);
        sb.Append(' ');
        sb.Append(reset);
        sb.Append(title);
        sb.Append(borderColor);
        sb.Append(' ');
        sb.Append(Horizontal, totalDashesAfterTitle);
        sb.Append(TopRight);
        sb.AppendLine(reset);
    }

    private static void AppendContentLine(StringBuilder sb, string line, int innerWidth, string borderColor, string reset)
    {
        // │ line + padding │
        var padding = innerWidth - line.Length - 2; // -2 for leading space + trailing space

        sb.Append(borderColor);
        sb.Append(Vertical);
        sb.Append(reset);
        sb.Append(' ');
        sb.Append(line);
        sb.Append(' ', padding > 0 ? padding : 0);
        sb.Append(' ');
        sb.Append(borderColor);
        sb.Append(Vertical);
        sb.AppendLine(reset);
    }

    private static void AppendSeparator(StringBuilder sb, int innerWidth, string borderColor, string reset)
    {
        // ├──────────────────┤
        sb.Append(borderColor);
        sb.Append(SeparatorLeft);
        sb.Append(Horizontal, innerWidth);
        sb.Append(SeparatorRight);
        sb.AppendLine(reset);
    }

    private static void AppendBottomBorder(StringBuilder sb, int innerWidth, string borderColor, string reset)
    {
        // ╰──────────────────╯
        sb.Append(borderColor);
        sb.Append(BottomLeft);
        sb.Append(Horizontal, innerWidth);
        sb.Append(BottomRight);
        sb.AppendLine(reset);
    }
}
