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
        BranchLabelInfo branchLabel,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        GitStatusCounts gitStatusCounts,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons!;

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

        var labelWithOp = AppendOperationToBranchLabel(branchLabel, operationName);

        var branchColor = branchLabel.State switch
        {
            BranchState.NoUpstream => ColorBranchNoUpstream,
            BranchState.Detached => ColorBranchDetached,
            _ => ColorBranch,
        };

        statusBuilder.Append(branchColor).Append(labelWithOp).Append(ColorReset);

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

        if (stashEntryCount > 0 && (ConfigReader.Config.ShowStash ?? true))
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(stashIcon).Append(stashEntryCount).Append(ColorReset);
        }

        return statusBuilder.ToString();
    }

    internal static string BuildDisplayCompact(
        BranchLabelInfo branchLabel,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        bool isDirty,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons!;
        var aheadIcon = icons.Ahead ?? IconAhead.ToString();
        var behindIcon = icons.Behind ?? IconBehind.ToString();
        var dirtyIcon = icons.Dirty ?? IconDirty.ToString();
        var cleanIcon = icons.Clean ?? IconClean.ToString();
        var stashIcon = icons.Stash ?? IconStash.ToString();

        var statusBuilder = new StringBuilder();

        var labelWithOp = AppendOperationToBranchLabel(branchLabel, operationName);

        var branchColor = branchLabel.State switch
        {
            BranchState.NoUpstream => ColorBranchNoUpstream,
            BranchState.Detached => ColorBranchDetached,
            _ => ColorBranch,
        };

        statusBuilder.Append(branchColor).Append(labelWithOp).Append(ColorReset);

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

        if (stashEntryCount > 0 && (ConfigReader.Config.ShowStash ?? true))
        {
            statusBuilder.Append(' ').Append(ColorStash).Append(stashIcon).Append(stashEntryCount).Append(ColorReset);
        }

        return statusBuilder.ToString();
    }

    internal static BranchLabelInfo BuildBranchLabel(string branchName, BranchState state = BranchState.Normal)
    {
        var icons = ConfigReader.Config.Icons!;

        var (open, close) = state switch
        {
            BranchState.NoUpstream => (
                icons.BranchLabelOpenNoUpstream ?? icons.BranchLabelOpen ?? NoUpstreamBranchLabelOpen,
                icons.BranchLabelCloseNoUpstream ?? icons.BranchLabelClose ?? NoUpstreamBranchLabelClose),
            BranchState.Detached => (
                icons.BranchLabelOpenDetached ?? icons.BranchLabelOpen ?? DetachedBranchLabelOpen,
                icons.BranchLabelCloseDetached ?? icons.BranchLabelClose ?? DetachedBranchLabelClose),
            _ => (
                icons.BranchLabelOpenNormal ?? icons.BranchLabelOpen ?? NormalBranchLabelOpen,
                icons.BranchLabelCloseNormal ?? icons.BranchLabelClose ?? NormalBranchLabelClose),
        };

        var prefix = state switch
        {
            BranchState.NoUpstream => icons.NoUpstreamMarker ?? NoUpstreamBranchMarker,
            BranchState.Detached => icons.DetachedHeadMarker ?? DetachedHeadBranchMarker,
            _ => string.Empty,
        };

        return new BranchLabelInfo($"{prefix}{open}{branchName}{close}", state);
    }

    private static string AppendOperationToBranchLabel(BranchLabelInfo branchLabel, string operationName)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return branchLabel.Label;
        }

        var icons = ConfigReader.Config.Icons!;
        var close = branchLabel.State switch
        {
            BranchState.NoUpstream => icons.BranchLabelCloseNoUpstream ?? icons.BranchLabelClose ?? NoUpstreamBranchLabelClose,
            BranchState.Detached => icons.BranchLabelCloseDetached ?? icons.BranchLabelClose ?? DetachedBranchLabelClose,
            _ => icons.BranchLabelCloseNormal ?? icons.BranchLabelClose ?? NormalBranchLabelClose,
        };
        var separator = icons.BranchOperationSeparator ?? BranchOperationSeparator;
        if (branchLabel.Label.EndsWith(close, StringComparison.Ordinal))
        {
            return branchLabel.Label[..^close.Length] + separator + operationName + close;
        }

        return branchLabel.Label + separator + operationName;
    }

    private static void AppendCountIndicators(StringBuilder sb, params CountStyle[] items)
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

