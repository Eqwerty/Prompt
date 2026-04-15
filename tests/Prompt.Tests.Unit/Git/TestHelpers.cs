using FluentAssertions;
using static Prompt.Constants.BranchLabelTokens;
using static Prompt.Constants.PromptColors;

namespace Prompt.Tests.Unit.Git;

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

    internal static string TrackedBranchLabel(string branchName) => $"{BranchLabelOpen}{branchName}{BranchLabelClose}";

    internal static string NoUpstreamBranchLabel(string branchName) => $"{NoUpstreamBranchMarker}{TrackedBranchLabel(branchName)}";

    internal static string BranchLabelWithOperation(string branchLabel, string operation) =>
        branchLabel.Replace(BranchLabelClose, $"|{operation}{BranchLabelClose}", StringComparison.Ordinal);

    internal static string Indicator(char icon, int count) => $"{icon}{count}";

    internal static string Colored(string color, string segment) => $"{color}{segment}{ColorReset}";
}

internal sealed class FakeTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan elapsed) => _utcNow = _utcNow.Add(elapsed);
}
