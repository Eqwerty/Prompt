using FluentAssertions;
using GitPrompt.Git;
using static GitPrompt.Constants.BranchLabelTokens;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Tests.Unit.Git;

internal static class TestHelpers
{
    internal static void AssertInOrder(string value, params string[] tokens)
    {
        var currentIndex = -1;
        foreach (var token in tokens)
        {
            var tokenIndex = value.IndexOf(token, StringComparison.Ordinal);
            tokenIndex.Should().BeGreaterThan(currentIndex, $"expected '{token}' to appear after previous indicators");
            currentIndex = tokenIndex;
        }
    }

    internal static BranchLabelInfo TrackedBranchLabel(string branchName) =>
        new($"{NormalBranchLabelOpen}{branchName}{NormalBranchLabelClose}", BranchState.Normal);

    internal static BranchLabelInfo NoUpstreamBranchLabel(string branchName) =>
        new($"{NoUpstreamBranchMarker}{NoUpstreamBranchLabelOpen}{branchName}{NoUpstreamBranchLabelClose}", BranchState.NoUpstream);

    internal static BranchLabelInfo DetachedBranchLabel(string branchLabel) =>
        new($"{DetachedHeadBranchMarker}{DetachedBranchLabelOpen}{branchLabel}{DetachedBranchLabelClose}", BranchState.Detached);

    internal static string BranchLabelWithOperation(BranchLabelInfo branchLabel, string operation) =>
        BranchLabelWithOperation(branchLabel.Label, operation);

    internal static string BranchLabelWithOperation(string branchLabel, string operation) =>
        branchLabel.Replace(BranchLabelClose, $"{BranchOperationSeparator}{operation}{BranchLabelClose}", StringComparison.Ordinal);

    internal static string Indicator(char icon, int count) => $"{icon}{count}";

    internal static string Indicator(string icon, int count) => $"{icon}{count}";

    internal static string Colored(string color, BranchLabelInfo segment) => Colored(color, segment.Label);

    internal static string Colored(string color, string segment) => $"{color}{segment}{ColorReset}";
}

internal sealed class FakeTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan elapsed) => _utcNow = _utcNow.Add(elapsed);
}
