namespace Prompt.Git;

internal sealed record GitStatusCounts(
    int StagedAdded = 0,
    int StagedModified = 0,
    int StagedDeleted = 0,
    int StagedRenamed = 0,
    int UnstagedAdded = 0,
    int UnstagedModified = 0,
    int UnstagedDeleted = 0,
    int UnstagedRenamed = 0,
    int Untracked = 0,
    int Conflicts = 0);
