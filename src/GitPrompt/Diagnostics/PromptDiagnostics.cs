using System.Runtime.CompilerServices;
using System.Text;
using GitPrompt.Configuration;
using GitPrompt.Prompting;

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
    private static int _gitSubprocessTimeoutCount;

    private static ConfigLoadResult? _configLoadResult;

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
        _gitSubprocessTimeoutCount = 0;
        _configLoadResult = null;
    }

    internal static IDisposable EnableForTesting()
    {
        Enable();
        Reset();

        return new DiagnosticsScope();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordConfigLoaded(ConfigLoadResult result)
    {
        if (!IsEnabled)
        {
            return;
        }

        _configLoadResult = result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordRepoCacheL1Hit()
    {
        if (!IsEnabled)
        {
            return;
        }

        _repoCacheHit = true;
        _repoCacheL1Hit = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordRepoCacheL2Hit()
    {
        if (!IsEnabled)
        {
            return;
        }

        _repoCacheHit = true;
        _repoCacheL1Hit = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordGitSubprocessElapsed(TimeSpan elapsed)
    {
        if (!IsEnabled)
        {
            return;
        }

        _gitSubprocessElapsed += elapsed;
        _gitSubprocessCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordGitSubprocessTimeout(TimeSpan elapsed)
    {
        if (!IsEnabled)
        {
            return;
        }

        _gitSubprocessElapsed += elapsed;
        _gitSubprocessCount++;
        _gitSubprocessTimeoutCount++;
    }

    internal static string GetReport(string directory, PromptResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("GitPrompt — diagnostic report");
        sb.AppendLine();

        var displayDirectory = directory.Replace('\\', '/');
        sb.AppendLine($"  {"Directory",-9}  {displayDirectory}");

        var displayPrompt = StripAnsi(result.PromptLine);
        if (!string.IsNullOrEmpty(displayPrompt))
        {
            sb.AppendLine($"  {"Prompt",-9}  {displayPrompt} {result.PromptSymbol}");
        }

        sb.AppendLine();
        sb.AppendLine($"  {"Context",-9}  {FormatMs(result.ContextElapsed)}");
        sb.AppendLine($"  {"Git",-9}  {FormatMs(result.GitElapsed)}");

        sb.Append($"    {"Repository",-10}  ");
        AppendRepoCacheStatus(sb);

        sb.Append($"    {"Status",-10}  ");
        AppendStatusCacheStatus(sb);

        sb.AppendLine($"  {"Total",-9}  {FormatMs(result.TotalElapsed)}");

        if (_configLoadResult is not null)
        {
            sb.AppendLine();
            AppendConfigSection(sb);
        }

        sb.AppendLine();
        sb.Append(BuildSummary());

        return sb.ToString();
    }

    private static void AppendConfigSection(StringBuilder sb)
    {
        if (_configLoadResult is null)
        {
            return;
        }

        var displayPath = _configLoadResult.FilePath.Replace('\\', '/');
        sb.AppendLine($"  {"Config",-9}  {displayPath}");

        var statusText = _configLoadResult.Status switch
        {
            ConfigLoadStatus.Loaded => "loaded",
            ConfigLoadStatus.Missing => "missing (using defaults)",
            ConfigLoadStatus.ParseFailed => "invalid JSON (using defaults)",
            ConfigLoadStatus.ReadFailed => "read error (using defaults)",
            _ => "unknown"
        };
        sb.AppendLine($"    {"Status",-10}  {statusText}");

        var config = _configLoadResult.Config;
        var gitStatusTtlText = config.Cache.GitStatusTtl == TimeSpan.Zero
            ? "0s (disabled)"
            : FormatSeconds(config.Cache.GitStatusTtl);
        var repoTtlText = config.Cache.RepositoryTtl == TimeSpan.Zero
            ? "0s (disabled)"
            : FormatSeconds(config.Cache.RepositoryTtl);
        sb.AppendLine($"    {"TTL",-10}  gitStatus {gitStatusTtlText} · repo {repoTtlText}");

        var timeoutText = config.CommandTimeout.HasValue
            ? FormatMs(config.CommandTimeout.Value)
            : "disabled";
        sb.AppendLine($"    {"Timeout",-10}  {timeoutText}");
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
            ? _gitSubprocessTimeoutCount > 0
                ? $" → ran git ({FormatMs(_gitSubprocessElapsed)}) [timed out]"
                : $" → ran git ({FormatMs(_gitSubprocessElapsed)})"
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
