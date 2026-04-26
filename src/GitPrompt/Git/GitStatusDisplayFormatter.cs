using System.Text;
using static GitPrompt.Constants.BranchLabelTokens;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Constants.PromptIcons;

namespace GitPrompt.Git;

internal static class GitStatusDisplayFormatter
{
    private readonly record struct CountStyle(int Value, string Color, char Icon);

    internal static string BuildDisplay(
        string branchDescription,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        GitStatusCounts gitStatusCounts,
        string operationName)
    {
        var statusBuilder = new StringBuilder();

        branchDescription = AppendOperationToBranchLabel(branchDescription, operationName);

        var noUpstreamPrefix = NoUpstreamBranchMarker + BranchLabelOpen;
        var branchColor = branchDescription.StartsWith(noUpstreamPrefix, StringComparison.Ordinal)
            ? ColorBranchNoUpstream
            : ColorBranch;

        statusBuilder.Append(branchColor).Append(branchDescription).Append(ColorReset);

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead).Append(IconAhead).Append(commitsAhead).Append(ColorReset);
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind).Append(IconBehind).Append(commitsBehind).Append(ColorReset);
        }

        AppendCountIndicators(
            statusBuilder,
            new CountStyle(gitStatusCounts.StagedAdded, ColorStaged, IconAdded),
            new CountStyle(gitStatusCounts.StagedModified, ColorStaged, IconModified),
            new CountStyle(gitStatusCounts.StagedRenamed, ColorStaged, IconRenamed),
            new CountStyle(gitStatusCounts.StagedDeleted, ColorStaged, IconDeleted),
            new CountStyle(gitStatusCounts.UnstagedAdded, ColorUnstaged, IconAdded),
            new CountStyle(gitStatusCounts.UnstagedModified, ColorUnstaged, IconModified),
            new CountStyle(gitStatusCounts.UnstagedRenamed, ColorUnstaged, IconRenamed),
            new CountStyle(gitStatusCounts.UnstagedDeleted, ColorUnstaged, IconDeleted)
        );

        if (gitStatusCounts.Untracked > 0)
        {
            statusBuilder.Append(' ').Append(ColorUntracked).Append(IconUntracked).Append(gitStatusCounts.Untracked).Append(ColorReset);
        }

        if (gitStatusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorConflict).Append(IconConflicts).Append(gitStatusCounts.Conflicts).Append(ColorReset);
        }

        if (stashEntryCount > 0)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(IconStash).Append(stashEntryCount).Append(ColorReset);
        }

        return statusBuilder.ToString();
    }

    internal static string BuildBranchLabel(string branchName, bool hasUpstream = true)
    {
        var noUpstreamPrefix = hasUpstream ? string.Empty : NoUpstreamBranchMarker;

        return $"{noUpstreamPrefix}{BranchLabelOpen}{branchName}{BranchLabelClose}";
    }

    private static string AppendOperationToBranchLabel(string branchLabel, string operationName)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return branchLabel;
        }

        const string branchOperationSeparator = "|";
        if (branchLabel.EndsWith(BranchLabelClose, StringComparison.Ordinal))
        {
            return branchLabel[..^BranchLabelClose.Length] + branchOperationSeparator + operationName + BranchLabelClose;
        }

        return branchLabel + branchOperationSeparator + operationName;
    }

    private static void AppendCountIndicators(StringBuilder sb, params ReadOnlySpan<CountStyle> items)
    {
        foreach (var item in items)
        {
            if (item.Value > 0)
            {
                sb.Append(' ').Append(item.Color).Append(item.Icon).Append(item.Value).Append(ColorReset);
            }
        }
    }
}
