using System.Runtime.InteropServices;
using System.Text;
using GitPrompt.Configuration;
using GitPrompt.Platform;

namespace GitPrompt.Git;

internal static class GitRepositorySharedCache
{
    private const string CacheDirectoryName = "repository-cache";
    private static readonly TimeSpan StaleCacheEntryThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static TimeProvider _timeProvider = TimeProvider.System;
    private static string? _cacheDirectoryOverride;
    private static long _nextCleanupUtcTicks;

    internal static bool TryGet(string startDirectoryPath, out GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        repositoryContext = default;

        try
        {
            var normalizedStartDirectoryPath = NormalizePathOrEmpty(startDirectoryPath);
            if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
            if (!File.Exists(cacheFilePath))
            {
                return false;
            }

            var cacheContent = File.ReadAllText(cacheFilePath);
            if (!TryParseRecord(cacheContent, out var cacheRecord))
            {
                return false;
            }

            if (!string.Equals(cacheRecord.StartDirectoryPath, normalizedStartDirectoryPath, Utilities.FileSystemPathComparison))
            {
                return false;
            }

            if (IsExpired(cacheRecord.CachedAtUtcTicks))
            {
                return false;
            }

            repositoryContext = new GitRepositoryLocator.RepositoryContext(cacheRecord.WorkingTreePath, cacheRecord.GitDirectoryPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static void Set(IEnumerable<string> startDirectoryPaths, GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        try
        {
            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            var cachedAtUtcTicks = GetUtcNow().Ticks;
            foreach (var startDirectoryPath in startDirectoryPaths)
            {
                var normalizedStartDirectoryPath = NormalizePathOrEmpty(startDirectoryPath);
                if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
                {
                    continue;
                }

                var cacheRecord = new RepositorySharedCacheRecord(
                    normalizedStartDirectoryPath,
                    NormalizePathOrEmpty(repositoryContext.WorkingTreePath),
                    NormalizePathOrEmpty(repositoryContext.GitDirectoryPath),
                    cachedAtUtcTicks);

                var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
                WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
            }

            TryCleanupStaleEntries(cacheDirectoryPath);
        }
        catch
        {
            // Keep cache as a best-effort optimization.
        }
    }

    private static void WriteAtomically(string targetFilePath, string[] lines)
    {
        var tempFilePath = targetFilePath + "." + Path.GetRandomFileName() + ".tmp";

        try
        {
            File.WriteAllLines(tempFilePath, lines);
            File.Move(tempFilePath, targetFilePath, overwrite: true);
            tempFilePath = null;
        }
        finally
        {
            if (tempFilePath is not null)
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception)
                {
                    /* best-effort temp file cleanup */
                }
            }
        }
    }

    private static bool IsExpired(long cachedAtUtcTicks)
    {
        var cacheTtl = GetCacheTtl();
        if (cacheTtl <= TimeSpan.Zero)
        {
            return true;
        }

        var cachedAtUtc = new DateTime(cachedAtUtcTicks, DateTimeKind.Utc);

        return GetUtcNow() - cachedAtUtc > cacheTtl;
    }

    private static void TryCleanupStaleEntries(string cacheDirectoryPath)
    {
        var utcNow = GetUtcNow();
        var nextCleanupUtcTicks = Interlocked.Read(ref _nextCleanupUtcTicks);
        if (utcNow.UtcTicks < nextCleanupUtcTicks)
        {
            return;
        }

        Interlocked.Exchange(ref _nextCleanupUtcTicks, utcNow.Add(CleanupInterval).UtcTicks);
        CleanupStaleEntries(cacheDirectoryPath, utcNow.UtcDateTime - StaleCacheEntryThreshold);
    }

    private static void CleanupStaleEntries(string cacheDirectoryPath, DateTime staleBeforeUtc)
    {
        foreach (var cacheFilePath in Directory.EnumerateFiles(cacheDirectoryPath, "*.cache"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(cacheFilePath) < staleBeforeUtc)
                {
                    File.Delete(cacheFilePath);
                }
            }
            catch
            {
                // Keep cleanup as best-effort and never fail prompt rendering.
            }
        }
    }

    internal static void ResetCleanupScheduleForTesting()
    {
        Interlocked.Exchange(ref _nextCleanupUtcTicks, 0);
    }

    internal static IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
    {
        var previousTimeProvider = _timeProvider;
        _timeProvider = timeProvider;

        return new TimeProviderOverride(() => _timeProvider = previousTimeProvider);
    }

    internal static IDisposable OverrideCacheDirectoryForTesting(string cacheDirectoryPath)
    {
        _cacheDirectoryOverride = cacheDirectoryPath;

        return new TimeProviderOverride(() => _cacheDirectoryOverride = null);
    }

    private static TimeSpan GetCacheTtl()
    {
        var ttl = ConfigReader.Config.Cache.RepositoryTtl;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    private static DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private static string GetCacheDirectoryPath()
    {
        return _cacheDirectoryOverride ?? Path.Combine(XdgPaths.GetCacheDirectory(), CacheDirectoryName);
    }

    private static string GetCacheFilePath(string normalizedStartDirectoryPath)
    {
        var pathHash = HashPath(normalizedStartDirectoryPath);

        return Path.Combine(GetCacheDirectoryPath(), pathHash + ".cache");
    }

    private static string HashPath(string value)
    {
        const int StackAllocThreshold = 512;
        var byteCount = Encoding.UTF8.GetByteCount(value);
        ulong hash;

        if (byteCount <= StackAllocThreshold)
        {
            Span<byte> bytes = stackalloc byte[StackAllocThreshold];
            var written = Encoding.UTF8.GetBytes(value.AsSpan(), bytes);
            hash = Fnv1a64(bytes[..written]);
        }
        else
        {
            hash = Fnv1a64(Encoding.UTF8.GetBytes(value));
        }

        Span<char> chars = stackalloc char[16];
        hash.TryFormat(chars, out _, "x16");
        return new string(chars);
    }

    private static ulong Fnv1a64(ReadOnlySpan<byte> data)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= prime;
        }
        return hash;
    }

    private static string NormalizePathOrEmpty(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : Utilities.NormalizePath(path);
    }

    private static string[] SerializeRecord(RepositorySharedCacheRecord cacheRecord)
    {
        return
        [
            "v1",
            cacheRecord.CachedAtUtcTicks.ToString(),
            Encode(cacheRecord.StartDirectoryPath),
            Encode(cacheRecord.WorkingTreePath),
            Encode(cacheRecord.GitDirectoryPath)
        ];
    }

    private static bool TryParseRecord(string fileContent, out RepositorySharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        var span = fileContent.AsSpan();

        if (!TryReadLine(ref span, out var versionLine) || !versionLine.Equals("v1", StringComparison.Ordinal))
            return false;

        if (!TryReadLine(ref span, out var ticksLine) || !long.TryParse(ticksLine, out var cachedAtUtcTicks))
            return false;

        if (!TryReadLine(ref span, out var startDirLine) ||
            !TryReadLine(ref span, out var workingTreeLine) ||
            !TryReadLine(ref span, out var gitDirLine))
            return false;

        var startDirectoryPath = Decode(startDirLine);
        var workingTreePath = Decode(workingTreeLine);
        var gitDirectoryPath = Decode(gitDirLine);
        if (string.IsNullOrEmpty(startDirectoryPath) || string.IsNullOrEmpty(workingTreePath) || string.IsNullOrEmpty(gitDirectoryPath))
            return false;

        cacheRecord = new RepositorySharedCacheRecord(
            startDirectoryPath,
            workingTreePath,
            gitDirectoryPath,
            cachedAtUtcTicks);

        return true;
    }

    private static bool TryReadLine(ref ReadOnlySpan<char> span, out ReadOnlySpan<char> line)
    {
        if (span.IsEmpty)
        {
            line = default;
            return false;
        }

        var newlineIndex = span.IndexOfAny('\r', '\n');
        if (newlineIndex < 0)
        {
            line = span;
            span = default;
            return true;
        }

        line = span[..newlineIndex];
        var next = span[(newlineIndex + 1)..];
        if (!next.IsEmpty && span[newlineIndex] == '\r' && next[0] == '\n')
            next = next[1..];
        span = next;
        return true;
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Decode(ReadOnlySpan<char> encoded)
    {
        try
        {
            const int StackAllocThreshold = 512;
            var maxByteCount = ((encoded.Length + 3) / 4) * 3;
            if (maxByteCount <= StackAllocThreshold)
            {
                Span<byte> buffer = stackalloc byte[StackAllocThreshold];
                if (!Convert.TryFromBase64Chars(encoded, buffer, out var bytesWritten))
                    return string.Empty;
                return Encoding.UTF8.GetString(buffer[..bytesWritten]);
            }

            var bytes = new byte[maxByteCount];
            if (!Convert.TryFromBase64Chars(encoded, bytes, out var written))
                return string.Empty;
            return Encoding.UTF8.GetString(bytes, 0, written);
        }
        catch
        {
            return string.Empty;
        }
    }

    private readonly record struct RepositorySharedCacheRecord(
        string StartDirectoryPath,
        string WorkingTreePath,
        string GitDirectoryPath,
        long CachedAtUtcTicks);

    private sealed class TimeProviderOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose()
        {
            _restore();
        }
    }
}
