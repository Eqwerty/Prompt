using BenchmarkDotNet.Attributes;
using Prompt.Git;

namespace Prompt.Benchmarks;

[MemoryDiagnoser]
public class GitStatusParserBenchmarks
{
    private readonly string _statusOutput = """
                                            # branch.oid 1234567890abcdef1234567890abcdef12345678
                                            # branch.head main
                                            # branch.upstream origin/main
                                            # branch.ab +3 -2
                                            1 A. file-a
                                            1 .M file-b
                                            2 R. file-c file-c-renamed
                                            2 .R file-d file-d-renamed
                                            1 D. file-e
                                            1 .D file-f
                                            ? untracked.txt
                                            u UU conflict.txt
                                            """;

    [Benchmark]
    public int ParseGitStatus()
    {
        return GitStatusParser.Parse(_statusOutput).CommitsAhead;
    }
}
