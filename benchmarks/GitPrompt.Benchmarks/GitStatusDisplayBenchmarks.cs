using BenchmarkDotNet.Attributes;
using GitPrompt.Git;

namespace GitPrompt.Benchmarks;

[MemoryDiagnoser]
public class GitStatusDisplayBenchmarks
{
    private string _gitDirectoryPath = string.Empty;

    private GitStatusCounts _cleanCounts = new();

    private GitStatusCounts _busyCounts = new(
        StagedAdded: 1,
        StagedModified: 2,
        StagedRenamed: 1,
        UnstagedModified: 2,
        UnstagedDeleted: 1,
        Untracked: 3,
        Conflicts: 1);

    [GlobalSetup]
    public void Setup()
    {
        _gitDirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Benchmarks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_gitDirectoryPath);

        _cleanCounts = new GitStatusCounts();

        _busyCounts = new GitStatusCounts(
            StagedAdded: 1,
            StagedModified: 2,
            StagedRenamed: 1,
            UnstagedModified: 2,
            UnstagedDeleted: 1,
            Untracked: 3,
            Conflicts: 1);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_gitDirectoryPath))
        {
            Directory.Delete(_gitDirectoryPath, recursive: true);
        }
    }

    [Benchmark]
    public string BuildDisplay_CleanRepository()
    {
        return GitStatusDisplayFormatter.BuildDisplay("(main)", commitsAhead: 0, commitsBehind: 0, stashEntryCount: 0, _cleanCounts, _gitDirectoryPath);
    }

    [Benchmark]
    public string BuildDisplay_BusyRepository()
    {
        return GitStatusDisplayFormatter.BuildDisplay("(feature)", commitsAhead: 4, commitsBehind: 2, stashEntryCount: 0, _busyCounts, _gitDirectoryPath);
    }
}
