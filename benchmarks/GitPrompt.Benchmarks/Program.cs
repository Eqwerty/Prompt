using BenchmarkDotNet.Running;
using GitPrompt.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(GitStatusParserBenchmarks).Assembly).Run(args);
