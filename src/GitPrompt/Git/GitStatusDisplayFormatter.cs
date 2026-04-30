using System.Text;
using GitPrompt.Configuration;
using static GitPrompt.Constants.BranchLabelTokens;
using static GitPrompt.Constants.PromptColors;
using static GitPrompt.Constants.PromptIcons;

namespace GitPrompt.Git;

internal static class GitStatusDisplayFormatter
{
    private readonly record struct CountStyle(int Value, string Color, string Icon);

    internal static string BuildDisplay(
        string branchDescription,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        GitStatusCounts gitStatusCounts,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons;

        var aheadIcon = icons.Ahead ?? IconAhead.ToString();
        var behindIcon = icons.Behind ?? IconBehind.ToString();
        var addedIcon = icons.Added ?? IconAdded.ToString();
        var modifiedIcon = icons.Modified ?? IconModified.ToString();
        var renamedIcon = icons.Renamed ?? IconRenamed.ToString();
        var deletedIcon = icons.Deleted ?? IconDeleted.ToString();
        var untrackedIcon = icons.Untracked ?? IconUntracked.ToString();
        var conflictsIcon = icons.Conflicts ?? IconConflicts.ToString();
        var stashIcon = icons.Stash ?? IconStash.ToString();

        var statusBuilder = new StringBuilder();

        branchDescription = AppendOperationToBranchLabel(branchDescription, operationName);

        var noUpstreamPrefix = NoUpstreamBranchMarker + BranchLabelOpen;
        var branchColor = branchDescription.StartsWith(noUpstreamPrefix, StringComparison.Ordinal)
            ? ColorBranchNoUpstream
            : ColorBranch;

        statusBuilder.Append(branchColor).Append(branchDescription).Append(ColorReset);

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead).Append(aheadIcon).Append(commitsAhead).Append(ColorReset);
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind).Append(behindIcon).Append(commitsBehind).Append(ColorReset);
        }

        AppendCountIndicators(
            statusBuilder,
            new CountStyle(gitStatusCounts.StagedAdded, ColorStaged, addedIcon),
            new CountStyle(gitStatusCounts.StagedModified, ColorStaged, modifiedIcon),
            new CountStyle(gitStatusCounts.StagedRenamed, ColorStaged, renamedIcon),
            new CountStyle(gitStatusCounts.StagedDeleted, ColorStaged, deletedIcon),
            new CountStyle(gitStatusCounts.UnstagedAdded, ColorUnstaged, addedIcon),
            new CountStyle(gitStatusCounts.UnstagedModified, ColorUnstaged, modifiedIcon),
            new CountStyle(gitStatusCounts.UnstagedRenamed, ColorUnstaged, renamedIcon),
            new CountStyle(gitStatusCounts.UnstagedDeleted, ColorUnstaged, deletedIcon)
        );

        if (gitStatusCounts.Untracked > 0)
        {
            statusBuilder.Append(' ').Append(ColorUntracked).Append(untrackedIcon).Append(gitStatusCounts.Untracked).Append(ColorReset);
        }

        if (gitStatusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorConflict).Append(conflictsIcon).Append(gitStatusCounts.Conflicts).Append(ColorReset);
        }

        if (stashEntryCount > 0)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(stashIcon).Append(stashEntryCount).Append(ColorReset);
        }

        return statusBuilder.ToString();
    }

    internal static string BuildDisplayCompact(
        string branchDescription,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        bool isDirty,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons;
        var aheadIcon = icons.Ahead ?? IconAhead.ToString();
        var behindIcon = icons.Behind ?? IconBehind.ToString();
        var dirtyIcon = icons.Dirty ?? IconDirty.ToString();
        var cleanIcon = icons.Clean ?? IconClean.ToString();
        var stashIcon = icons.Stash ?? IconStash.ToString();

        var statusBuilder = new StringBuilder();

        branchDescription = AppendOperationToBranchLabel(branchDescription, operationName);

        var noUpstreamPrefix = NoUpstreamBranchMarker + BranchLabelOpen;
        var branchColor = branchDescription.StartsWith(noUpstreamPrefix, StringComparison.Ordinal)
            ? ColorBranchNoUpstream
            : ColorBranch;

        statusBuilder.Append(branchColor).Append(branchDescription).Append(ColorReset);

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead).Append(aheadIcon).Append(commitsAhead).Append(ColorReset);
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind).Append(behindIcon).Append(commitsBehind).Append(ColorReset);
        }

        if (isDirty)
        {
            statusBuilder.Append(' ').Append(ColorDirty).Append(dirtyIcon).Append(ColorReset);
        }
        else
        {
            statusBuilder.Append(' ').Append(ColorClean).Append(cleanIcon).Append(ColorReset);
        }

        if (stashEntryCount > 0 && ConfigReader.Config.ShowStashInCompactMode)
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(stashIcon).Append(stashEntryCount).Append(ColorReset);
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
