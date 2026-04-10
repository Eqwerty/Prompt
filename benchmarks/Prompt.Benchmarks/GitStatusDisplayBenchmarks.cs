using BenchmarkDotNet.Attributes;
using Prompt.Git;

namespace Prompt.Benchmarks;

[MemoryDiagnoser]
public class GitStatusDisplayBenchmarks
{
    private string _gitDirectoryPath = string.Empty;

    private StatusCounts _cleanCounts = new(
        StagedAdded: 0,
        StagedModified: 0,
        StagedDeleted: 0,
        StagedRenamed: 0,
        UnstagedAdded: 0,
        UnstagedModified: 0,
        UnstagedDeleted: 0,
        UnstagedRenamed: 0,
        Untracked: 0,
        Conflicts: 0);

    private StatusCounts _busyCounts = new(
        StagedAdded: 1,
        StagedModified: 2,
        StagedDeleted: 0,
        StagedRenamed: 1,
        UnstagedAdded: 0,
        UnstagedModified: 2,
        UnstagedDeleted: 1,
        UnstagedRenamed: 0,
        Untracked: 3,
        Conflicts: 1);

    [GlobalSetup]
    public void Setup()
    {
        _gitDirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Benchmarks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_gitDirectoryPath);

        _cleanCounts = new StatusCounts(
            StagedAdded: 0,
            StagedModified: 0,
            StagedDeleted: 0,
            StagedRenamed: 0,
            UnstagedAdded: 0,
            UnstagedModified: 0,
            UnstagedDeleted: 0,
            UnstagedRenamed: 0,
            Untracked: 0,
            Conflicts: 0);

        _busyCounts = new StatusCounts(
            StagedAdded: 1,
            StagedModified: 2,
            StagedDeleted: 0,
            StagedRenamed: 1,
            UnstagedAdded: 0,
            UnstagedModified: 2,
            UnstagedDeleted: 1,
            UnstagedRenamed: 0,
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
        return GitStatusSegmentBuilder.BuildDisplay("(main)", commitsAhead: 0, commitsBehind: 0, _cleanCounts, _gitDirectoryPath);
    }

    [Benchmark]
    public string BuildDisplay_BusyRepository()
    {
        return GitStatusSegmentBuilder.BuildDisplay("(feature)", commitsAhead: 4, commitsBehind: 2, _busyCounts, _gitDirectoryPath);
    }
}
