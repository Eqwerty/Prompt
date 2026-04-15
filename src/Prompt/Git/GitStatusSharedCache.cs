using System.Security.Cryptography;
using System.Text;

namespace Prompt.Git;

internal static class GitStatusSharedCache
{
    private const string CacheDirectoryEnvironmentVariable = "PROMPT_GIT_STATUS_CACHE_DIR";
    private const string CacheTtlMillisecondsEnvironmentVariable = "PROMPT_GIT_STATUS_CACHE_TTL_MS";
    private const string InvalidationTokenFileName = "status-invalidation.token";

    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleCacheEntryThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static TimeProvider _timeProvider = TimeProvider.System;
    private static long _nextCleanupUtcTicks;

    internal static bool TryGet(string repositoryRootPath, string gitDirectoryPath, out string segment)
    {
        segment = string.Empty;

        try
        {
            if (!IsCacheEnabled())
            {
                return false;
            }

            var normalizedRepositoryRootPath = NormalizePathOrEmpty(repositoryRootPath);
            var normalizedGitDirectoryPath = NormalizePathOrEmpty(gitDirectoryPath);
            if (string.IsNullOrEmpty(normalizedRepositoryRootPath) || string.IsNullOrEmpty(normalizedGitDirectoryPath))
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(normalizedRepositoryRootPath);
            if (!File.Exists(cacheFilePath))
            {
                return false;
            }

            var cacheLines = File.ReadAllLines(cacheFilePath);
            if (!TryParseRecord(cacheLines, out var cacheRecord))
            {
                return false;
            }

            if (!string.Equals(cacheRecord.RepositoryRootPath, normalizedRepositoryRootPath, Utilities.FileSystemPathComparison))
            {
                return false;
            }

            if (IsExpired(cacheRecord.CachedAtUtcTicks))
            {
                return false;
            }

            var currentInvalidationTokenValue = ReadInvalidationTokenValue();
            if (!string.Equals(cacheRecord.InvalidationTokenValue, currentInvalidationTokenValue, StringComparison.Ordinal))
            {
                return false;
            }

            var currentFingerprint = BuildFingerprint(normalizedGitDirectoryPath);
            if (!string.Equals(cacheRecord.Fingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                return false;
            }

            segment = cacheRecord.Segment;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static void Set(string repositoryRootPath, string gitDirectoryPath, string segment)
    {
        try
        {
            if (!IsCacheEnabled() || string.IsNullOrEmpty(segment))
            {
                return;
            }

            var normalizedRepositoryRootPath = NormalizePathOrEmpty(repositoryRootPath);
            var normalizedGitDirectoryPath = NormalizePathOrEmpty(gitDirectoryPath);
            if (string.IsNullOrEmpty(normalizedRepositoryRootPath) || string.IsNullOrEmpty(normalizedGitDirectoryPath))
            {
                return;
            }

            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            var cacheRecord = new GitStatusSharedCacheRecord(
                normalizedRepositoryRootPath,
                BuildFingerprint(normalizedGitDirectoryPath),
                segment,
                GetUtcNow().Ticks,
                ReadInvalidationTokenValue());

            var cacheFilePath = GetCacheFilePath(normalizedRepositoryRootPath);
            WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
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
                try { File.Delete(tempFilePath); }
                catch (Exception)
                {
                    /* best-effort temp file cleanup */
                }
            }
        }
    }

    private static bool IsCacheEnabled()
    {
        return GetCacheTtl() > TimeSpan.Zero;
    }

    private static bool IsExpired(long cachedAtUtcTicks)
    {
        var cacheTtl = GetCacheTtl();
        if (cacheTtl <= TimeSpan.Zero)
        {
            return true;
        }

        var cachedAtUtc = new DateTimeOffset(cachedAtUtcTicks, TimeSpan.Zero);
        return GetUtcNow() - cachedAtUtc > cacheTtl;
    }

    private static TimeSpan GetCacheTtl()
    {
        var configuredValue = Environment.GetEnvironmentVariable(CacheTtlMillisecondsEnvironmentVariable);
        if (int.TryParse(configuredValue, out var ttlMilliseconds))
        {
            return ttlMilliseconds > 0 ? TimeSpan.FromMilliseconds(ttlMilliseconds) : TimeSpan.Zero;
        }

        return DefaultCacheTtl;
    }

    internal static void Invalidate()
    {
        try
        {
            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            // A random token value avoids filesystem timestamp precision issues.
            File.WriteAllText(GetInvalidationTokenPath(), Guid.NewGuid().ToString("N"));
        }
        catch
        {
            // Keep cache invalidation as best-effort.
        }
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

    internal static IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
    {
        var previousTimeProvider = _timeProvider;
        _timeProvider = timeProvider;

        return new TimeProviderOverride(() => _timeProvider = previousTimeProvider);
    }

    internal static void ResetCleanupScheduleForTesting()
    {
        Interlocked.Exchange(ref _nextCleanupUtcTicks, 0);
    }

    private static DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private sealed class TimeProviderOverride(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;

        public void Dispose()
        {
            _restore();
        }
    }

    private static string GetCacheDirectoryPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(CacheDirectoryEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Path.GetTempPath(), "Prompt", "git-status-cache-v1")
            : configuredPath;
    }

    private static string GetCacheFilePath(string normalizedRepositoryRootPath)
    {
        return Path.Combine(GetCacheDirectoryPath(), HashPath(normalizedRepositoryRootPath) + ".cache");
    }

    private static string GetInvalidationTokenPath()
    {
        return Path.Combine(GetCacheDirectoryPath(), InvalidationTokenFileName);
    }

    private static string ReadInvalidationTokenValue()
    {
        try
        {
            var tokenPath = GetInvalidationTokenPath();
            if (!File.Exists(tokenPath))
            {
                return string.Empty;
            }

            return File.ReadAllText(tokenPath);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildFingerprint(string normalizedGitDirectoryPath)
    {
        var commonGitDirectoryPath = ResolveCommonGitDirectoryPath(normalizedGitDirectoryPath);
        var builder = new StringBuilder();

        builder.Append("COMMON_GIT_DIR:").Append(commonGitDirectoryPath).Append(';');

        AppendStamp(builder, normalizedGitDirectoryPath, "HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "index");
        AppendStamp(builder, commonGitDirectoryPath, "packed-refs");
        AppendStamp(builder, commonGitDirectoryPath, Path.Combine("refs", "stash"));
        AppendStamp(builder, commonGitDirectoryPath, "FETCH_HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "MERGE_HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "REBASE_HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "CHERRY_PICK_HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "REVERT_HEAD");
        AppendStamp(builder, normalizedGitDirectoryPath, "BISECT_LOG");

        var headRefPath = TryResolveHeadRefPath(normalizedGitDirectoryPath, commonGitDirectoryPath);
        if (!string.IsNullOrEmpty(headRefPath))
        {
            AppendAbsoluteFileStamp(builder, headRefPath, "HEAD_REF");

            var upstreamRefPath = TryResolveUpstreamRefPath(commonGitDirectoryPath, headRefPath);
            if (!string.IsNullOrEmpty(upstreamRefPath))
            {
                AppendAbsoluteFileStamp(builder, upstreamRefPath, "UPSTREAM_REF");
            }
        }

        return HashPath(builder.ToString());
    }

    private static string ResolveCommonGitDirectoryPath(string normalizedGitDirectoryPath)
    {
        try
        {
            var commonDirFilePath = Path.Combine(normalizedGitDirectoryPath, "commondir");
            if (!File.Exists(commonDirFilePath))
            {
                return normalizedGitDirectoryPath;
            }

            var commonDirValue = File.ReadLines(commonDirFilePath).FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(commonDirValue))
            {
                return normalizedGitDirectoryPath;
            }

            var resolvedCommonDirPath = Path.IsPathRooted(commonDirValue)
                ? commonDirValue
                : Path.GetFullPath(Path.Combine(normalizedGitDirectoryPath, commonDirValue));

            return Directory.Exists(resolvedCommonDirPath)
                ? Utilities.NormalizePath(resolvedCommonDirPath)
                : normalizedGitDirectoryPath;
        }
        catch
        {
            return normalizedGitDirectoryPath;
        }
    }

    private static string? TryResolveHeadRefPath(string normalizedGitDirectoryPath, string normalizedCommonGitDirectoryPath)
    {
        try
        {
            var headPath = Path.Combine(normalizedGitDirectoryPath, "HEAD");
            if (!File.Exists(headPath))
            {
                return null;
            }

            var headLine = File.ReadLines(headPath).FirstOrDefault()?.Trim();
            const string refPrefix = "ref:";
            if (string.IsNullOrEmpty(headLine) || !headLine.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relativeRefPath = headLine[refPrefix.Length..].Trim();
            if (string.IsNullOrEmpty(relativeRefPath))
            {
                return null;
            }

            var normalizedRelativeRefPath = relativeRefPath.Replace('/', Path.DirectorySeparatorChar);

            var worktreeRefPath = Utilities.NormalizePath(Path.Combine(normalizedGitDirectoryPath, normalizedRelativeRefPath));
            if (File.Exists(worktreeRefPath))
            {
                return worktreeRefPath;
            }

            return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, normalizedRelativeRefPath));
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveUpstreamRefPath(string normalizedCommonGitDirectoryPath, string normalizedHeadRefPath)
    {
        try
        {
            var branchName = TryExtractBranchNameFromHeadRefPath(normalizedCommonGitDirectoryPath, normalizedHeadRefPath);
            if (string.IsNullOrEmpty(branchName))
            {
                return null;
            }

            if (!TryReadBranchTrackingConfiguration(normalizedCommonGitDirectoryPath, branchName, out var remoteName, out var mergeReference))
            {
                return null;
            }

            if (string.Equals(remoteName, ".", StringComparison.Ordinal))
            {
                return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, mergeReference.Replace('/', Path.DirectorySeparatorChar)));
            }

            if (mergeReference.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase))
            {
                var branchRelativePath = mergeReference["refs/heads/".Length..];
                return Utilities.NormalizePath(Path.Combine(
                    normalizedCommonGitDirectoryPath,
                    "refs",
                    "remotes",
                    remoteName,
                    branchRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            }

            if (mergeReference.StartsWith("refs/remotes/", StringComparison.OrdinalIgnoreCase))
            {
                return Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, mergeReference.Replace('/', Path.DirectorySeparatorChar)));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractBranchNameFromHeadRefPath(string normalizedCommonGitDirectoryPath, string normalizedHeadRefPath)
    {
        var refsHeadsPrefix = Utilities.NormalizePath(Path.Combine(normalizedCommonGitDirectoryPath, "refs", "heads") + Path.DirectorySeparatorChar);
        if (!normalizedHeadRefPath.StartsWith(refsHeadsPrefix, Utilities.FileSystemPathComparison))
        {
            return null;
        }

        return normalizedHeadRefPath[refsHeadsPrefix.Length..].Replace(Path.DirectorySeparatorChar, '/');
    }

    private static bool TryReadBranchTrackingConfiguration(
        string normalizedCommonGitDirectoryPath,
        string branchName,
        out string remoteName,
        out string mergeReference)
    {
        remoteName = string.Empty;
        mergeReference = string.Empty;

        var configPath = Path.Combine(normalizedCommonGitDirectoryPath, "config");
        if (!File.Exists(configPath))
        {
            return false;
        }

        string? activeBranchSection = null;
        foreach (var rawLine in File.ReadLines(configPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                activeBranchSection = TryParseBranchSection(line);
                continue;
            }

            if (!string.Equals(activeBranchSection, branchName, StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (string.Equals(key, "remote", StringComparison.OrdinalIgnoreCase))
            {
                remoteName = value;
            }
            else if (string.Equals(key, "merge", StringComparison.OrdinalIgnoreCase))
            {
                mergeReference = value;
            }

            if (!string.IsNullOrEmpty(remoteName) && !string.IsNullOrEmpty(mergeReference))
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryParseBranchSection(string sectionLine)
    {
        var section = sectionLine[1..^1].Trim();
        if (!section.StartsWith("branch", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var firstQuoteIndex = section.IndexOf('"');
        var lastQuoteIndex = section.LastIndexOf('"');
        if (firstQuoteIndex < 0 || lastQuoteIndex <= firstQuoteIndex)
        {
            return null;
        }

        return section[(firstQuoteIndex + 1)..lastQuoteIndex];
    }

    private static void AppendStamp(StringBuilder builder, string gitDirectoryPath, string relativePath)
    {
        var absolutePath = Path.Combine(gitDirectoryPath, relativePath);
        AppendAbsoluteFileStamp(builder, absolutePath, relativePath);
    }

    private static void AppendAbsoluteFileStamp(StringBuilder builder, string filePath, string label)
    {
        var exists = File.Exists(filePath);
        if (!exists)
        {
            builder.Append(label).Append(':').Append('0').Append(';');
            return;
        }

        var fileInfo = new FileInfo(filePath);
        builder.Append(label).Append(':')
            .Append('1').Append(':')
            .Append(fileInfo.Length).Append(':')
            .Append(fileInfo.LastWriteTimeUtc.Ticks).Append(';');
    }

    private static string HashPath(string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(valueBytes);

        var builder = new StringBuilder(hashBytes.Length * 2);
        foreach (var hashByte in hashBytes)
        {
            builder.Append(hashByte.ToString("x2"));
        }

        return builder.ToString();
    }

    private static string NormalizePathOrEmpty(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : Utilities.NormalizePath(path);
    }

    private static string[] SerializeRecord(GitStatusSharedCacheRecord cacheRecord)
    {
        return
        [
            "v3",
            cacheRecord.CachedAtUtcTicks.ToString(),
            Encode(cacheRecord.RepositoryRootPath),
            Encode(cacheRecord.Fingerprint),
            Encode(cacheRecord.Segment),
            Encode(cacheRecord.InvalidationTokenValue)
        ];
    }

    private static bool TryParseRecord(string[] lines, out GitStatusSharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        if (lines.Length < 6 || !string.Equals(lines[0], "v3", StringComparison.Ordinal))
        {
            return false;
        }

        if (!long.TryParse(lines[1], out var cachedAtUtcTicks))
        {
            return false;
        }

        var repositoryRootPath = Decode(lines[2]);
        var fingerprint = Decode(lines[3]);
        var segment = Decode(lines[4]);
        var invalidationTokenValue = Decode(lines[5]);
        if (string.IsNullOrEmpty(repositoryRootPath) || string.IsNullOrEmpty(fingerprint))
        {
            return false;
        }

        cacheRecord = new GitStatusSharedCacheRecord(repositoryRootPath, fingerprint, segment, cachedAtUtcTicks, invalidationTokenValue);
        return true;
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Decode(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            return string.Empty;
        }
    }

    private readonly record struct GitStatusSharedCacheRecord(
        string RepositoryRootPath,
        string Fingerprint,
        string Segment,
        long CachedAtUtcTicks,
        string InvalidationTokenValue);
}
