using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Unit.Git;

public sealed class GitStatusParserTests
{
    [Fact]
    public void Parse_WhenStatusContainsAheadBehindAndCounters_ShouldParseSnapshotValues()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.oid 1234567890abcdef1234567890abcdef12345678
                                    # branch.head master
                                    # branch.upstream origin/master
                                    # branch.ab +3 -2
                                    # stash 4
                                    1 A. ignored
                                    1 .M ignored
                                    2 R. ignored
                                    2 .R ignored
                                    1 D. ignored
                                    1 .D ignored
                                    ? untracked.txt
                                    u UU ignored
                                    """;

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.BranchHeadName.Should().Be("master");
        gitStatusSnapshot.HeadObjectId.Should().Be("1234567890abcdef1234567890abcdef12345678");
        gitStatusSnapshot.UpstreamReference.Should().Be("origin/master");
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(3);
        gitStatusSnapshot.CommitsBehind.Should().Be(2);
        gitStatusSnapshot.StashEntryCount.Should().Be(4);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    [Fact]
    public void Parse_WhenStatusContainsAllSupportedCodesWithoutUpstream_ShouldTrackCountsAndNoUpstreamState()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.oid abcdef1234567890abcdef1234567890abcdef12
                                    # branch.head feature
                                    1 AM file-a
                                    1 MD file-b
                                    2 R. file-c file-c-renamed
                                    2 .R file-d file-d-renamed
                                    1 .A file-e
                                    1 .D file-f
                                    ? untracked.txt
                                    u UU conflict.txt
                                    """;

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.HasUpstream.Should().BeFalse();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeFalse();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);
        gitStatusSnapshot.StashEntryCount.Should().Be(0);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.StagedModified.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(2);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    [Fact]
    public void Parse_WhenTrackedEntriesContainCopyConflictAndUnsupportedCodes_ShouldTrackOnlySupportedCounters()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.oid abcdef1234567890abcdef1234567890abcdef12
                                    # branch.head feature
                                    1 C. copy-staged
                                    1 .C copy-unstaged
                                    1 U. conflict-staged
                                    1 .U conflict-unstaged
                                    1 .. ignored
                                    1    ignored-spaces
                                    """;

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(0);
        statusCounts.StagedModified.Should().Be(0);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedAdded.Should().Be(0);
        statusCounts.UnstagedModified.Should().Be(0);
        statusCounts.UnstagedDeleted.Should().Be(0);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.Untracked.Should().Be(0);
        statusCounts.Conflicts.Should().Be(2);
    }
}
