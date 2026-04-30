namespace GitPrompt.Git;

internal sealed record GitStatusCompactSnapshot(
    string BranchHeadName,
    string HeadObjectId,
    int CommitsAhead,
    int CommitsBehind,
    int StashEntryCount,
    string UpstreamReference,
    bool HasUpstream,
    bool HasAheadBehindCounts,
    bool IsDirty);
