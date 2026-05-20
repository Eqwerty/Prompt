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

    internal static BranchLabelInfo TrackedBranchLabel(string branchName)
    {
        return new BranchLabelInfo($"{NormalBranchLabelOpen}{branchName}{NormalBranchLabelClose}", BranchState.Normal);
    }

    internal static BranchLabelInfo NoUpstreamBranchLabel(string branchName)
    {
        return new BranchLabelInfo($"{NoUpstreamBranchMarker}{NoUpstreamBranchLabelOpen}{branchName}{NoUpstreamBranchLabelClose}", BranchState.NoUpstream);
    }

    internal static BranchLabelInfo DetachedBranchLabel(string branchLabel)
    {
        return new BranchLabelInfo($"{DetachedHeadBranchMarker}{DetachedBranchLabelOpen}{branchLabel}{DetachedBranchLabelClose}", BranchState.Detached);
    }

    internal static string BranchLabelWithOperation(BranchLabelInfo branchLabel, string operation)
    {
        var close = branchLabel.State switch
        {
            BranchState.Detached => DetachedBranchLabelClose,
            BranchState.NoUpstream => NoUpstreamBranchLabelClose,
            _ => NormalBranchLabelClose
        };

        return branchLabel.Label.Replace(close, $"{BranchOperationSeparator}{operation}{close}", StringComparison.Ordinal);
    }

    internal static string Indicator(char icon, int count)
    {
        return $"{icon}{count}";
    }

    internal static string Indicator(string icon, int count)
    {
        return $"{icon}{count}";
    }

    internal static string Colored(string color, BranchLabelInfo segment)
    {
        return Colored(color, segment.Label);
    }

    internal static string Colored(string color, string segment)
    {
        return $"{color}{segment}{ColorReset}";
    }
}
