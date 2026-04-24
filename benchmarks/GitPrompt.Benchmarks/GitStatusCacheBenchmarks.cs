using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using GitPrompt.Configuration;
using GitPrompt.Git;

namespace GitPrompt.Benchmarks;

[MemoryDiagnoser]
public class GitStatusCacheBenchmarks
{
    private string _sandboxPath = string.Empty;
    private string _cachePath = string.Empty;
    private string _repositoryPath = string.Empty;
    private string _gitDirectoryPath = string.Empty;
    private string _uncachedPath = string.Empty;

    private IDisposable? _configOverride;
    private IDisposable? _cacheDirectoryOverride;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _sandboxPath = Path.Combine(Path.GetTempPath(), "Prompt.Benchmarks", "GitStatusCache", Guid.NewGuid().ToString("N"));
        _cachePath = Path.Combine(_sandboxPath, "cache");
        Directory.CreateDirectory(_sandboxPath);
        Directory.CreateDirectory(_cachePath);

        _repositoryPath = Path.Combine(_sandboxPath, "repo");
        await RunGitAsync(_sandboxPath, $"init --initial-branch=main \"{_repositoryPath}\"");
        await RunGitAsync(_repositoryPath, "config user.name \"Cache Benchmarks\"");
        await RunGitAsync(_repositoryPath, "config user.email \"cache-benchmarks@example.com\"");
        await File.WriteAllTextAsync(Path.Combine(_repositoryPath, "readme.txt"), "hello\n");
        await RunGitAsync(_repositoryPath, "add readme.txt");
        await RunGitAsync(_repositoryPath, "commit -m \"initial\"");
        _gitDirectoryPath = Path.Combine(_repositoryPath, ".git");

        // Use an isolated cache directory and a long TTL so the cached entry stays valid for
        // the entire benchmark run.
        _configOverride = ConfigReader.OverrideForTesting(new Config
        {
            Cache = new Config.CacheConfig { GitStatusTtlSeconds = 3600.0 }
        });

        _cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(_cachePath);

        // Pre-populate the cache so TryGet_CacheHit finds a valid, unexpired entry.
        GitStatusSharedCache.Set(_repositoryPath, _gitDirectoryPath, "(main)");

        // A separate path with no cache entry, for the cache-miss benchmark.
        _uncachedPath = Path.Combine(_sandboxPath, "uncached");
        Directory.CreateDirectory(_uncachedPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cacheDirectoryOverride?.Dispose();
        _configOverride?.Dispose();

        if (!Directory.Exists(_sandboxPath))
        {
            return;
        }

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(_sandboxPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            Directory.Delete(_sandboxPath, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
    }

    [Benchmark]
    public bool TryGet_CacheHit()
    {
        return GitStatusSharedCache.TryGet(_repositoryPath, _gitDirectoryPath, out _);
    }

    [Benchmark]
    public bool TryGet_CacheMiss_FileNotFound()
    {
        return GitStatusSharedCache.TryGet(_uncachedPath, _gitDirectoryPath, out _);
    }

    [Benchmark]
    public void Set()
    {
        GitStatusSharedCache.Set(_repositoryPath, _gitDirectoryPath, "(main)");
    }

    private static async Task RunGitAsync(string workingDirectory, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        await process.WaitForExitAsync();
    }
}
