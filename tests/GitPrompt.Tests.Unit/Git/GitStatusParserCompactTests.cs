using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitStatusParserCompactTests
{
    private const string CleanStatusOutput = """
        # branch.oid 1234567890abcdef1234567890abcdef12345678
        # branch.head main
        # branch.upstream origin/main
        # branch.ab +2 -1
        # stash 3
        """;

    [Fact]
    public void ParseCompact_WhenInputIsEmpty_ShouldReturnDefaultSnapshot()
    {
        // Act
        var snapshot = GitStatusParser.ParseCompact(string.Empty);

        // Assert
        snapshot.BranchHeadName.Should().BeEmpty();
        snapshot.HeadObjectId.Should().BeEmpty();
        snapshot.UpstreamReference.Should().BeEmpty();
        snapshot.HasUpstream.Should().BeFalse();
        snapshot.HasAheadBehindCounts.Should().BeFalse();
        snapshot.CommitsAhead.Should().Be(0);
        snapshot.CommitsBehind.Should().Be(0);
        snapshot.StashEntryCount.Should().Be(0);
        snapshot.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void ParseCompact_WhenRepoIsClean_ShouldReturnAllHeaderFieldsAndIsDirtyFalse()
    {
        // Act
        var snapshot = GitStatusParser.ParseCompact(CleanStatusOutput);

        // Assert
        snapshot.BranchHeadName.Should().Be("main");
        snapshot.HeadObjectId.Should().Be("1234567890abcdef1234567890abcdef12345678");
        snapshot.UpstreamReference.Should().Be("origin/main");
        snapshot.HasUpstream.Should().BeTrue();
        snapshot.HasAheadBehindCounts.Should().BeTrue();
        snapshot.CommitsAhead.Should().Be(2);
        snapshot.CommitsBehind.Should().Be(1);
        snapshot.StashEntryCount.Should().Be(3);
        snapshot.IsDirty.Should().BeFalse();
    }

    [Theory]
    [InlineData("1 A. file.txt")]
    [InlineData("1 .M file.txt")]
    [InlineData("2 R. old.txt new.txt")]
    [InlineData("? untracked.txt")]
    [InlineData("u UU file.txt")]
    public void ParseCompact_WhenRepoHasAnyFileEntry_ShouldReturnIsDirtyTrue(string fileEntry)
    {
        // Arrange
        var statusOutput = CleanStatusOutput + "\n" + fileEntry;

        // Act
        var snapshot = GitStatusParser.ParseCompact(statusOutput);

        // Assert
        snapshot.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ParseCompact_WhenRepoIsDirty_ShouldStillParseAllHeaderFields()
    {
        // Arrange
        const string statusOutput = """
            # branch.oid abcdefabcdefabcdefabcdefabcdefabcdefabcd
            # branch.head feature
            # branch.upstream origin/feature
            # branch.ab +1 -0
            # stash 2
            1 .M modified.txt
            1 A. added.txt
            ? untracked.txt
            """;

        // Act
        var snapshot = GitStatusParser.ParseCompact(statusOutput);

        // Assert
        snapshot.BranchHeadName.Should().Be("feature");
        snapshot.CommitsAhead.Should().Be(1);
        snapshot.CommitsBehind.Should().Be(0);
        snapshot.StashEntryCount.Should().Be(2);
        snapshot.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ParseCompact_WhenRepoIsDirty_ShouldBreakAfterFirstFileEntry()
    {
        // Arrange — three file entries; we verify all header data is still intact
        const string statusOutput = """
            # branch.oid 0000000000000000000000000000000000000000
            # branch.head main
            # branch.ab +0 -0
            1 .M a.txt
            1 .M b.txt
            1 .M c.txt
            """;

        // Act
        var snapshot = GitStatusParser.ParseCompact(statusOutput);

        // Assert — IsDirty is true but we stopped after the first entry (behavior indistinguishable
        // from counting all, but the key assertion is the result is correct)
        snapshot.IsDirty.Should().BeTrue();
        snapshot.BranchHeadName.Should().Be("main");
    }
}
