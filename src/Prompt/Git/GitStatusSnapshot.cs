namespace Prompt.Git;

internal sealed record GitStatusSnapshot(
    string BranchHeadName,
    string HeadObjectId,
    int CommitsAhead,
    int CommitsBehind,
    int StashEntryCount,
    string UpstreamReference,
    bool HasUpstream,
    bool HasAheadBehindCounts,
    StatusCounts StatusCounts);
