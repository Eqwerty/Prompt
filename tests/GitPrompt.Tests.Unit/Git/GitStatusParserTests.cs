using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitStatusParserTests
{
    public enum CounterKind
    {
        None,
        StagedAdded,
        StagedModified,
        StagedDeleted,
        StagedRenamed,
        UnstagedAdded,
        UnstagedModified,
        UnstagedDeleted,
        UnstagedRenamed,
        Untracked,
        Conflicts
    }

    [Fact]
    public void Parse_WhenInputIsEmpty_ShouldReturnDefaultSnapshot()
    {
        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(string.Empty);

        // Assert
        gitStatusSnapshot.BranchHeadName.Should().BeEmpty();
        gitStatusSnapshot.HeadObjectId.Should().BeEmpty();
        gitStatusSnapshot.UpstreamReference.Should().BeEmpty();
        gitStatusSnapshot.HasUpstream.Should().BeFalse();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeFalse();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);
        gitStatusSnapshot.StashEntryCount.Should().Be(0);
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

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

        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts(
            StagedAdded: 1,
            StagedDeleted: 1,
            StagedRenamed: 1,
            UnstagedModified: 1,
            UnstagedDeleted: 1,
            UnstagedRenamed: 1,
            Untracked: 1,
            Conflicts: 1));
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

        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts(
            StagedAdded: 1,
            StagedModified: 1,
            StagedRenamed: 1,
            UnstagedAdded: 1,
            UnstagedModified: 1,
            UnstagedDeleted: 2,
            UnstagedRenamed: 1,
            Untracked: 1,
            Conflicts: 1));
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
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts(
            StagedRenamed: 1,
            UnstagedRenamed: 1,
            Conflicts: 2));
    }

    [Theory]
    [MemberData(nameof(TrackedStatusCodeCases))]
    public void Parse_WhenTrackedEntryContainsStatusPair_ShouldMapEveryCounterCorrectly(string statusPair, CounterKind expectedCounter, int expectedValue)
    {
        // Arrange
        var statusOutput = $"1 {statusPair} file.txt";
        var expectedCounts = CreateExpectedCounts(expectedCounter, expectedValue);

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.GitStatusCounts.Should().Be(expectedCounts);
    }

    [Theory]
    [InlineData("A.", CounterKind.StagedAdded)]
    [InlineData(".A", CounterKind.UnstagedAdded)]
    [InlineData("M.", CounterKind.StagedModified)]
    [InlineData(".M", CounterKind.UnstagedModified)]
    [InlineData("D.", CounterKind.StagedDeleted)]
    [InlineData(".D", CounterKind.UnstagedDeleted)]
    [InlineData("R.", CounterKind.StagedRenamed)]
    [InlineData(".R", CounterKind.UnstagedRenamed)]
    [InlineData("C.", CounterKind.StagedRenamed)]
    [InlineData(".C", CounterKind.UnstagedRenamed)]
    [InlineData("U.", CounterKind.Conflicts)]
    [InlineData(".U", CounterKind.Conflicts)]
    public void Parse_WhenMultipleEntriesShareTheSameStatusCode_ShouldAccumulateCounter(string statusPair, CounterKind expectedCounter)
    {
        // Arrange
        var statusOutput = string.Join('\n',
            $"1 {statusPair} file-a",
            $"1 {statusPair} file-b",
            $"1 {statusPair} file-c");

        var expectedCounts = CreateExpectedCounts(expectedCounter, value: 3);

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.GitStatusCounts.Should().Be(expectedCounts);
    }

    [Fact]
    public void Parse_WhenInputContainsUnsupportedOrShortTrackedRecords_ShouldIgnoreThem()
    {
        // Arrange
        const string statusOutput = """
                                    1
                                    1 A
                                    1 A
                                    3 AM unsupported-record-type
                                    ! ignored-by-git
                                    """;

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

    [Fact]
    public void Parse_WhenMetadataContainsAheadBehindWithoutUpstream_ShouldSetUpstreamAndAheadBehindFlags()
    {
        // Arrange
        const string statusOutput = "# branch.ab +4 -1";

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(4);
        gitStatusSnapshot.CommitsBehind.Should().Be(1);
        gitStatusSnapshot.UpstreamReference.Should().BeEmpty();
        gitStatusSnapshot.StashEntryCount.Should().Be(0);
        gitStatusSnapshot.BranchHeadName.Should().BeEmpty();
        gitStatusSnapshot.HeadObjectId.Should().BeEmpty();
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

    [Fact]
    public void Parse_WhenMetadataContainsMalformedAheadBehindAndStash_ShouldKeepDefaultNumericValues()
    {
        // Arrange
        const string statusOutput = "# branch.ab +x -y\n# stash not-a-number";

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);
        gitStatusSnapshot.StashEntryCount.Should().Be(0);
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

    [Fact]
    public void Parse_WhenMetadataContainsOnlyUpstream_ShouldSetUpstreamWithoutAheadBehind()
    {
        // Arrange
        const string statusOutput = "# branch.upstream origin/main";

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.UpstreamReference.Should().Be("origin/main");
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeFalse();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);
        gitStatusSnapshot.StashEntryCount.Should().Be(0);
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

    [Fact]
    public void Parse_WhenBranchHeaderLinesAreDuplicated_ShouldOverwriteWithLastSeenValue()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.head old
                                    # branch.head new
                                    # branch.oid oldoid
                                    # branch.oid newoid
                                    # branch.upstream origin/old
                                    # branch.upstream origin/new
                                    # branch.ab +1 -2
                                    # branch.ab +7 -3
                                    # stash 1
                                    # stash 9
                                    """;

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.BranchHeadName.Should().Be("new");
        gitStatusSnapshot.HeadObjectId.Should().Be("newoid");
        gitStatusSnapshot.UpstreamReference.Should().Be("origin/new");
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(7);
        gitStatusSnapshot.CommitsBehind.Should().Be(3);
        gitStatusSnapshot.StashEntryCount.Should().Be(9);
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts());
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Parse_WhenInputUsesDifferentLineTerminators_ShouldReadAllRecords(string lineTerminator)
    {
        // Arrange
        var statusOutput = string.Join(lineTerminator,
            "# branch.head feature/line-endings",
            "1 A. file-a",
            "1 .M file-b",
            "? untracked.txt",
            "u UU conflict.txt");

        // Act
        var gitStatusSnapshot = GitStatusParser.Parse(statusOutput);

        // Assert
        gitStatusSnapshot.BranchHeadName.Should().Be("feature/line-endings");
        gitStatusSnapshot.GitStatusCounts.Should().Be(new GitStatusCounts(
            StagedAdded: 1,
            UnstagedModified: 1,
            Untracked: 1,
            Conflicts: 1));
    }

    public static TheoryData<string, CounterKind, int> TrackedStatusCodeCases
    {
        get
        {
            return new TheoryData<string, CounterKind, int>
            {
                { "A.", CounterKind.StagedAdded, 1 },
                { ".A", CounterKind.UnstagedAdded, 1 },
                { "M.", CounterKind.StagedModified, 1 },
                { ".M", CounterKind.UnstagedModified, 1 },
                { "D.", CounterKind.StagedDeleted, 1 },
                { ".D", CounterKind.UnstagedDeleted, 1 },
                { "R.", CounterKind.StagedRenamed, 1 },
                { ".R", CounterKind.UnstagedRenamed, 1 },
                { "C.", CounterKind.StagedRenamed, 1 },
                { ".C", CounterKind.UnstagedRenamed, 1 },
                { "U.", CounterKind.Conflicts, 1 },
                { ".U", CounterKind.Conflicts, 1 },
                { "UU", CounterKind.Conflicts, 2 },
                { "..", CounterKind.None, 0 },
                { "X.", CounterKind.None, 0 },
                { ".X", CounterKind.None, 0 }
            };
        }
    }

    private static GitStatusCounts CreateExpectedCounts(CounterKind counter, int value)
    {
        return counter switch
        {
            CounterKind.None => new GitStatusCounts(),
            CounterKind.StagedAdded => new GitStatusCounts(StagedAdded: value),
            CounterKind.StagedModified => new GitStatusCounts(StagedModified: value),
            CounterKind.StagedDeleted => new GitStatusCounts(StagedDeleted: value),
            CounterKind.StagedRenamed => new GitStatusCounts(StagedRenamed: value),
            CounterKind.UnstagedAdded => new GitStatusCounts(UnstagedAdded: value),
            CounterKind.UnstagedModified => new GitStatusCounts(UnstagedModified: value),
            CounterKind.UnstagedDeleted => new GitStatusCounts(UnstagedDeleted: value),
            CounterKind.UnstagedRenamed => new GitStatusCounts(UnstagedRenamed: value),
            CounterKind.Untracked => new GitStatusCounts(Untracked: value),
            CounterKind.Conflicts => new GitStatusCounts(Conflicts: value),
            _ => throw new ArgumentOutOfRangeException(nameof(counter), counter, message: null)
        };
    }
}
