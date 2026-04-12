namespace Prompt.Git;

internal static class GitStatusParser
{
    private const string BranchHeadPrefix = "# branch.head ";
    private const string BranchOidPrefix = "# branch.oid ";
    private const string BranchAheadBehindPrefix = "# branch.ab ";
    private const string BranchUpstreamPrefix = "# branch.upstream ";
    private const string StashPrefix = "# stash ";
    private const string UntrackedRecordPrefix = "? ";
    private const string UnmergedRecordPrefix = "u ";

    private struct GitStatusCountsAccumulator
    {
        public int StagedAdded;
        public int StagedModified;
        public int StagedDeleted;
        public int StagedRenamed;
        public int UnstagedAdded;
        public int UnstagedModified;
        public int UnstagedDeleted;
        public int UnstagedRenamed;
        public int Untracked;
        public int Conflicts;

        public GitStatusCounts ToGitStatusCounts()
        {
            return new GitStatusCounts(
                StagedAdded,
                StagedModified,
                StagedDeleted,
                StagedRenamed,
                UnstagedAdded,
                UnstagedModified,
                UnstagedDeleted,
                UnstagedRenamed,
                Untracked,
                Conflicts);
        }
    }

    internal static GitStatusSnapshot Parse(string statusOutput)
    {
        var statusText = statusOutput.AsSpan();
        var branchHeadName = string.Empty;
        var headObjectId = string.Empty;
        var commitsAhead = 0;
        var commitsBehind = 0;
        var stashEntryCount = 0;
        var upstreamReference = string.Empty;
        var hasUpstream = false;
        var hasAheadBehindCounts = false;
        var statusCountsAccumulator = new GitStatusCountsAccumulator();

        while (TryReadLine(ref statusText, out var line))
        {
            if (line.IsEmpty)
            {
                continue;
            }

            if (TryParseBranchMetadata(
                    line,
                    ref branchHeadName,
                    ref headObjectId,
                    ref commitsAhead,
                    ref commitsBehind,
                    ref stashEntryCount,
                    ref upstreamReference,
                    ref hasUpstream,
                    ref hasAheadBehindCounts))
            {
                continue;
            }

            TrackStatusCounts(line, ref statusCountsAccumulator);
        }

        return new GitStatusSnapshot(branchHeadName,
            headObjectId,
            commitsAhead,
            commitsBehind,
            stashEntryCount,
            upstreamReference,
            hasUpstream,
            hasAheadBehindCounts,
            statusCountsAccumulator.ToGitStatusCounts());
    }

    private static bool TryParseBranchMetadata(
        ReadOnlySpan<char> line,
        ref string branchHeadName,
        ref string headObjectId,
        ref int commitsAhead,
        ref int commitsBehind,
        ref int stashEntryCount,
        ref string upstreamReference,
        ref bool hasUpstream,
        ref bool hasAheadBehindCounts)
    {
        if (line.StartsWith(BranchHeadPrefix.AsSpan(), StringComparison.Ordinal))
        {
            branchHeadName = line[BranchHeadPrefix.Length..].ToString();

            return true;
        }

        if (line.StartsWith(BranchOidPrefix.AsSpan(), StringComparison.Ordinal))
        {
            headObjectId = line[BranchOidPrefix.Length..].ToString();

            return true;
        }

        if (line.StartsWith(BranchAheadBehindPrefix.AsSpan(), StringComparison.Ordinal))
        {
            ParseAheadBehind(line[BranchAheadBehindPrefix.Length..], out commitsAhead, out commitsBehind);
            hasUpstream = true;
            hasAheadBehindCounts = true;

            return true;
        }

        if (line.StartsWith(BranchUpstreamPrefix.AsSpan(), StringComparison.Ordinal))
        {
            upstreamReference = line[BranchUpstreamPrefix.Length..].ToString();
            hasUpstream = true;

            return true;
        }

        if (line.StartsWith(StashPrefix.AsSpan(), StringComparison.Ordinal))
        {
            var stashValue = line[StashPrefix.Length..].Trim();
            _ = int.TryParse(stashValue, out stashEntryCount);

            return true;
        }

        return false;
    }

    private static void TrackStatusCounts(ReadOnlySpan<char> line, ref GitStatusCountsAccumulator gitStatusCounts)
    {
        var isUntrackedRecord = line.StartsWith(UntrackedRecordPrefix.AsSpan(), StringComparison.Ordinal);
        var isUnmergedRecord = line.StartsWith(UnmergedRecordPrefix.AsSpan(), StringComparison.Ordinal);
        if (isUntrackedRecord || isUnmergedRecord)
        {
            if (isUntrackedRecord)
            {
                gitStatusCounts.Untracked++;
            }
            else
            {
                gitStatusCounts.Conflicts++;
            }

            return;
        }

        var isOrdinaryTrackedEntryRecord = line.Length >= 4 && line[0] is '1';
        var isRenamedOrCopiedTrackedEntryRecord = line.Length >= 4 && line[0] is '2';
        if (!isOrdinaryTrackedEntryRecord && !isRenamedOrCopiedTrackedEntryRecord)
        {
            return;
        }

        TrackStatusCode(line[2], isStaged: true, ref gitStatusCounts);
        TrackStatusCode(line[3], isStaged: false, ref gitStatusCounts);
    }

    private static void TrackStatusCode(char value, bool isStaged, ref GitStatusCountsAccumulator gitStatusCounts)
    {
        switch (value, isStaged)
        {
            case (value: 'A', isStaged: true):
            {
                gitStatusCounts.StagedAdded++;
                break;
            }
            case (value: 'A', isStaged: false):
            {
                gitStatusCounts.UnstagedAdded++;
                break;
            }
            case (value: 'M', isStaged: true):
            {
                gitStatusCounts.StagedModified++;
                break;
            }
            case (value: 'M', isStaged: false):
            {
                gitStatusCounts.UnstagedModified++;
                break;
            }
            case (value: 'D', isStaged: true):
            {
                gitStatusCounts.StagedDeleted++;
                break;
            }
            case (value: 'D', isStaged: false):
            {
                gitStatusCounts.UnstagedDeleted++;
                break;
            }
            case (value: 'R' or 'C', isStaged: true):
            {
                gitStatusCounts.StagedRenamed++;
                break;
            }
            case (value: 'R' or 'C', isStaged: false):
            {
                gitStatusCounts.UnstagedRenamed++;
                break;
            }
            case (value: 'U', isStaged: _):
            {
                gitStatusCounts.Conflicts++;
                break;
            }
        }
    }

    private static void ParseAheadBehind(ReadOnlySpan<char> value, out int commitsAhead, out int commitsBehind)
    {
        commitsAhead = 0;
        commitsBehind = 0;

        var trimmedValue = value.Trim();
        var separatorIndex = trimmedValue.IndexOf(' ');
        if (separatorIndex < 0)
        {
            return;
        }

        var aheadToken = trimmedValue[..separatorIndex].Trim().TrimStart('+');
        var behindToken = trimmedValue[(separatorIndex + 1)..].Trim().TrimStart('-');

        _ = int.TryParse(aheadToken, out commitsAhead);
        _ = int.TryParse(behindToken, out commitsBehind);
    }

    private static bool TryReadLine(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> line)
    {
        if (text.IsEmpty)
        {
            line = ReadOnlySpan<char>.Empty;

            return false;
        }

        var lineTerminatorIndex = text.IndexOfAny('\r', '\n');
        if (lineTerminatorIndex < 0)
        {
            line = text;
            text = ReadOnlySpan<char>.Empty;

            return true;
        }

        line = text[..lineTerminatorIndex];

        var skipCount = 1;
        if (text[lineTerminatorIndex] is '\r' &&
            lineTerminatorIndex + 1 < text.Length &&
            text[lineTerminatorIndex + 1] is '\n')
        {
            skipCount = 2;
        }

        text = text[(lineTerminatorIndex + skipCount)..];

        return true;
    }
}
