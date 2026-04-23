using System.Text;

namespace GitPrompt.Diagnostics;

internal static class PromptDiagnostics
{
    private static bool _repoCacheHit;
    private static bool _repoCacheL1Hit;
    private static bool _repoCacheMissRecorded;
    private static RepoCacheMissReason _repoCacheMissReason;
    private static bool _repoCacheWalkRecorded;
    private static int _repoCacheDirsWalked;
    private static bool _repoCacheRepoFound;

    private static bool _statusCacheRecorded;
    private static bool _statusCacheHit;
    private static TimeSpan _statusCacheAge;
    private static TimeSpan _statusCacheTtl;
    private static StatusCacheMissReason _statusCacheMissReason;
    private static TimeSpan _statusCacheMissAge;

    private static TimeSpan _gitSubprocessElapsed;
    private static int _gitSubprocessCount;

    internal static bool IsEnabled { get; private set; }

    internal static void Enable() => IsEnabled = true;

    internal static void Reset()
    {
        _repoCacheHit = false;
        _repoCacheL1Hit = false;
        _repoCacheMissRecorded = false;
        _repoCacheMissReason = default;
        _repoCacheWalkRecorded = false;
        _repoCacheDirsWalked = 0;
        _repoCacheRepoFound = false;
        _statusCacheRecorded = false;
        _statusCacheHit = false;
        _statusCacheAge = default;
        _statusCacheTtl = default;
        _statusCacheMissReason = default;
        _statusCacheMissAge = default;
        _gitSubprocessElapsed = default;
        _gitSubprocessCount = 0;
    }

    internal static IDisposable EnableForTesting()
    {
        Enable();
        Reset();
        return new DiagnosticsScope();
    }

    internal static void RecordRepoCacheL1Hit()
    {
        if (!IsEnabled)
        {
            return;
        }

        _repoCacheHit = true;
        _repoCacheL1Hit = true;
    }

    internal static void RecordRepoCacheL2Hit()
    {
        if (!IsEnabled)
        {
            return;
        }

        _repoCacheHit = true;
        _repoCacheL1Hit = false;
    }

    internal static void RecordRepoCacheL2Miss(RepoCacheMissReason reason)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (_repoCacheMissRecorded && _repoCacheMissReason != RepoCacheMissReason.NoEntry)
        {
            return;
        }

        _repoCacheMissRecorded = true;
        _repoCacheMissReason = reason;
    }

    internal static void RecordRepoCacheWalk(int dirsWalked, bool repoFound)
    {
        if (!IsEnabled)
        {
            return;
        }

        _repoCacheWalkRecorded = true;
        _repoCacheDirsWalked = dirsWalked;
        _repoCacheRepoFound = repoFound;
    }

    internal static void RecordStatusCacheHit(TimeSpan age, TimeSpan ttl)
    {
        if (!IsEnabled)
        {
            return;
        }

        _statusCacheRecorded = true;
        _statusCacheHit = true;
        _statusCacheAge = age;
        _statusCacheTtl = ttl;
    }

    internal static void RecordStatusCacheMiss(StatusCacheMissReason reason, TimeSpan age = default, TimeSpan ttl = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        _statusCacheRecorded = true;
        _statusCacheHit = false;
        _statusCacheMissReason = reason;
        _statusCacheMissAge = age;
        _statusCacheTtl = ttl;
    }

    internal static void RecordGitSubprocessElapsed(TimeSpan elapsed)
    {
        if (!IsEnabled)
        {
            return;
        }

        _gitSubprocessElapsed += elapsed;
        _gitSubprocessCount++;
    }

    internal static string GetReport(
        string directory,
        string gitStatusSegment,
        TimeSpan contextElapsed,
        TimeSpan gitElapsed,
        TimeSpan totalElapsed)
    {
        var sb = new StringBuilder();

        sb.AppendLine("GitPrompt — diagnostic report");

        var displayDirectory = directory.Replace('\\', '/');
        sb.AppendLine($"  Directory         {displayDirectory}");

        if (!string.IsNullOrEmpty(gitStatusSegment))
        {
            sb.AppendLine($"  Prompt            {StripAnsi(gitStatusSegment)}");
        }

        sb.AppendLine();
        sb.AppendLine($"  Context segment   {FormatMs(contextElapsed)}");
        sb.AppendLine($"  Git segment       {FormatMs(gitElapsed)}");

        sb.Append("    Repository      ");
        AppendRepoCacheStatus(sb);

        sb.Append("    Status cache    ");
        AppendStatusCacheStatus(sb);

        sb.AppendLine($"  Total             {FormatMs(totalElapsed)}");
        sb.AppendLine();
        sb.Append(BuildSummary());

        return sb.ToString();
    }

    private static void AppendRepoCacheStatus(StringBuilder sb)
    {
        if (_repoCacheHit)
        {
            sb.AppendLine(_repoCacheL1Hit ? "hit (in-process)" : "hit (disk)");

            return;
        }

        if (_repoCacheWalkRecorded)
        {
            var dirs = _repoCacheDirsWalked;
            var dirWord = dirs is 1 ? "dir" : "dirs";

            var missReason = _repoCacheMissRecorded
                ? FormatRepoCacheMissReason(_repoCacheMissReason) + " → "
                : string.Empty;

            var repoSuffix = _repoCacheRepoFound ? string.Empty : ", no repo found";
            sb.AppendLine($"miss · {missReason}walked {dirs} {dirWord}{repoSuffix}");

            return;
        }

        sb.AppendLine("(not recorded)");
    }

    private static void AppendStatusCacheStatus(StringBuilder sb)
    {
        if (!_statusCacheRecorded)
        {
            sb.AppendLine("skipped");

            return;
        }

        if (_statusCacheHit)
        {
            sb.AppendLine($"hit ({FormatSeconds(_statusCacheAge)} old · TTL {FormatSeconds(_statusCacheTtl)})");

            return;
        }

        var missDescription = _statusCacheMissReason switch
        {
            StatusCacheMissReason.Disabled => "cache disabled",
            StatusCacheMissReason.NoEntry => "no entry",
            StatusCacheMissReason.ParseError => "corrupt cache",
            StatusCacheMissReason.TtlExpired => $"TTL expired ({FormatSeconds(_statusCacheMissAge)} old · TTL {FormatSeconds(_statusCacheTtl)})",
            StatusCacheMissReason.FingerprintChanged => "git state changed",
            StatusCacheMissReason.InvalidationToken => "invalidated",
            _ => "unknown"
        };

        var gitRunSuffix = _gitSubprocessCount > 0
            ? $" → ran git ({FormatMs(_gitSubprocessElapsed)})"
            : string.Empty;

        sb.AppendLine($"miss · {missDescription}{gitRunSuffix}");
    }

    private static string BuildSummary()
    {
        if (!_statusCacheRecorded)
        {
            if (_repoCacheWalkRecorded && !_repoCacheRepoFound)
            {
                return "  Not in a git repository.\n";
            }

            return string.Empty;
        }

        if (_statusCacheHit)
        {
            return "  Git segment served from cache.\n";
        }

        return _statusCacheMissReason switch
        {
            StatusCacheMissReason.FingerprintChanged =>
                "  Cache miss caused by a real git change — TTL is not the issue.\n",
            StatusCacheMissReason.TtlExpired =>
                $"  Tip: if you see this often without making git changes, consider\n" +
                $"    increasing cache.gitStatusTtl in your config (current: {FormatSeconds(_statusCacheTtl)}).\n",
            StatusCacheMissReason.InvalidationToken =>
                "  Cache was explicitly invalidated (e.g. by a git hook).\n",
            StatusCacheMissReason.NoEntry =>
                "  First render or cache was evicted. Cache will be populated after this run.\n",
            StatusCacheMissReason.ParseError =>
                "  Cache file was corrupt and will be replaced.\n",
            StatusCacheMissReason.Disabled =>
                "  Status cache is disabled (cache.gitStatusTtl is 0 in your config).\n",
            _ => string.Empty
        };
    }

    private static string FormatRepoCacheMissReason(RepoCacheMissReason reason) => reason switch
    {
        RepoCacheMissReason.Disabled => "cache disabled",
        RepoCacheMissReason.NoEntry => "no entry",
        RepoCacheMissReason.ParseError => "corrupt entry",
        RepoCacheMissReason.TtlExpired => "TTL expired",
        _ => "miss"
    };

    private static string FormatMs(TimeSpan elapsed) => $"{(long)elapsed.TotalMilliseconds}ms";

    private static string FormatSeconds(TimeSpan span) => $"{(long)span.TotalSeconds}s";

    private static string StripAnsi(string text)
    {
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] is '\x1b' && i + 1 < text.Length && text[i + 1] is '[')
            {
                i += 2;
                while (i < text.Length && text[i] is not 'm')
                {
                    i++;
                }

                if (i < text.Length)
                {
                    i++;
                }
            }
            else
            {
                sb.Append(text[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private sealed class DiagnosticsScope : IDisposable
    {
        public void Dispose()
        {
            IsEnabled = false;
            Reset();
        }
    }
}
