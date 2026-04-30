using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitStatusCountsTests
{
    [Fact]
    public void IsDirty_WhenAllCountsAreZero_ShouldReturnFalse()
    {
        // Arrange & Act
        var counts = new GitStatusCounts();

        // Assert
        counts.IsDirty.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 1, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 1, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 1, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 1, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 1, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    public void IsDirty_WhenAnyCountIsNonZero_ShouldReturnTrue(
        int stagedAdded, int stagedModified, int stagedDeleted, int stagedRenamed,
        int unstagedAdded, int unstagedModified, int unstagedDeleted, int unstagedRenamed,
        int untracked, int conflicts)
    {
        // Arrange & Act
        var counts = new GitStatusCounts(
            stagedAdded, stagedModified, stagedDeleted, stagedRenamed,
            unstagedAdded, unstagedModified, unstagedDeleted, unstagedRenamed,
            untracked, conflicts);

        // Assert
        counts.IsDirty.Should().BeTrue();
    }
}
