using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using GitPrompt.Git;
using GitPrompt.Platform;
using GitPrompt.Prompting;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Benchmarks;

[MemoryDiagnoser]
public class PromptEndToEndBenchmarks
{

    private string _sandboxRootPath = string.Empty;
    private string _outsideRepositoryPath = string.Empty;
    private string _normalRepositoryPath = string.Empty;
    private string _nestedRepositoryPath = string.Empty;
    private string _worktreePath = string.Empty;
    private string _detachedHeadPath = string.Empty;
    private string _noUpstreamPath = string.Empty;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _sandboxRootPath = Path.Combine(Path.GetTempPath(), "Prompt.Benchmarks", "PromptEndToEnd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandboxRootPath);

        _outsideRepositoryPath = Path.Combine(_sandboxRootPath, "outside");
        Directory.CreateDirectory(_outsideRepositoryPath);

        (_normalRepositoryPath, _nestedRepositoryPath) = await CreateNormalRepositoryScenarioAsync();
        _worktreePath = await CreateWorktreeScenarioAsync();
        _detachedHeadPath = await CreateDetachedHeadScenarioAsync();
        _noUpstreamPath = await CreateNoUpstreamScenarioAsync();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (!Directory.Exists(_sandboxRootPath))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(_sandboxRootPath, searchPattern: "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        Directory.Delete(_sandboxRootPath, recursive: true);
    }

    [Benchmark]
    public Task<string> BuildPrompt_NotInGitRepository()
    {
        return BuildPromptAsync(_outsideRepositoryPath);
    }

    [Benchmark]
    public Task<string> BuildPrompt_NormalRepositoryRoot()
    {
        return BuildPromptAsync(_normalRepositoryPath);
    }

    [Benchmark]
    public Task<string> BuildPrompt_NestedSubdirectory()
    {
        return BuildPromptAsync(_nestedRepositoryPath);
    }

    [Benchmark]
    public Task<string> BuildPrompt_Worktree()
    {
        return BuildPromptAsync(_worktreePath);
    }

    [Benchmark]
    public Task<string> BuildPrompt_DetachedHeadWithSingleMatchingRemoteReference()
    {
        return BuildPromptAsync(_detachedHeadPath);
    }

    [Benchmark]
    public Task<string> BuildPrompt_NoUpstreamBranchPath()
    {
        return BuildPromptAsync(_noUpstreamPath);
    }

    private async Task<string> CreateWorktreeScenarioAsync()
    {
        var repositoryPath = Path.Combine(_sandboxRootPath, "worktree-repo");
        var worktreePath = Path.Combine(_sandboxRootPath, "worktree-feature");

        await RunGitAsync(_sandboxRootPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await RunGitAsync(repositoryPath, "add base.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        await RunGitAsync(repositoryPath, $"worktree add -b feature {Quote(worktreePath)}");

        await File.WriteAllTextAsync(Path.Combine(worktreePath, "feature.txt"), "feature\n");
        await RunGitAsync(worktreePath, "add feature.txt");
        await RunGitAsync(worktreePath, "commit -m \"feature\"");

        return worktreePath;
    }

    private async Task<string> CreateDetachedHeadScenarioAsync()
    {
        var remoteRepositoryPath = Path.Combine(_sandboxRootPath, "detached-remote.git");
        var sourceRepositoryPath = Path.Combine(_sandboxRootPath, "detached-source");
        var localRepositoryPath = Path.Combine(_sandboxRootPath, "detached-local");

        await RunGitAsync(_sandboxRootPath, $"init --bare --initial-branch=main {Quote(remoteRepositoryPath)}");
        await RunGitAsync(_sandboxRootPath, $"clone {Quote(remoteRepositoryPath)} {Quote(sourceRepositoryPath)}");
        await ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await RunGitAsync(sourceRepositoryPath, "add base.txt");
        await RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        var commitObjectId = (await RunGitAsync(sourceRepositoryPath, "rev-parse HEAD")).Trim();
        await RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await RunGitAsync(_sandboxRootPath, $"clone {Quote(remoteRepositoryPath)} {Quote(localRepositoryPath)}");
        await RunGitAsync(localRepositoryPath, $"checkout --detach {commitObjectId}");

        return localRepositoryPath;
    }

    private async Task<(string RepositoryPath, string NestedPath)> CreateNormalRepositoryScenarioAsync()
    {
        var repositoryPath = Path.Combine(_sandboxRootPath, "normal-repo");
        var nestedPath = Path.Combine(repositoryPath, "src", "features");

        await RunGitAsync(_sandboxRootPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "tracked\n");
        await RunGitAsync(repositoryPath, "add tracked.txt");
        await RunGitAsync(repositoryPath, "commit -m \"base\"");

        Directory.CreateDirectory(nestedPath);

        return (repositoryPath, nestedPath);
    }

    private async Task<string> CreateNoUpstreamScenarioAsync()
    {
        var repositoryPath = Path.Combine(_sandboxRootPath, "no-upstream");

        await RunGitAsync(_sandboxRootPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        await ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "main.txt"), "main\n");
        await RunGitAsync(repositoryPath, "add main.txt");
        await RunGitAsync(repositoryPath, "commit -m \"main base\"");

        await RunGitAsync(repositoryPath, "checkout -b feature");

        for (var i = 0; i < 120; i++)
        {
            await File.AppendAllTextAsync(Path.Combine(repositoryPath, "feature.txt"), $"line-{i}\n");
            await RunGitAsync(repositoryPath, "add feature.txt");
            await RunGitAsync(repositoryPath, $"commit -m \"feature {i}\"");
        }

        for (var i = 0; i < 240; i++)
        {
            await RunGitAsync(repositoryPath, $"tag perf-tag-{i} HEAD");
        }

        return repositoryPath;
    }

    private static async Task<string> BuildPromptAsync(string workingDirectoryPath)
    {
        var platformProvider = PlatformProvider.System;

        var contextSegment = ContextSegmentBuilder.Build(platformProvider);
        var gitStatusSegment = await GitStatusSegmentBuilder.BuildAsync(workingDirectoryPath);
        var promptSymbol = PromptSymbolBuilder.Build(platformProvider);

        var promptLine = string.IsNullOrEmpty(gitStatusSegment)
            ? contextSegment
            : $"{contextSegment} {gitStatusSegment}";

        return $"{promptLine}\n{ColorPromptSymbol}{promptSymbol}{ColorReset} ";
    }

    private static async Task ConfigureGitIdentityAsync(string repositoryPath)
    {
        await RunGitAsync(repositoryPath, "config user.name \"Prompt Benchmarks\"");
        await RunGitAsync(repositoryPath, "config user.email \"prompt-benchmarks@example.com\"");
    }

    private static async Task<string> RunGitAsync(string workingDirectoryPath, string arguments)
    {
        var commandResult = await RunGitAllowFailureAsync(workingDirectoryPath, arguments);
        if (commandResult.ExitCode is not 0)
        {
            throw new InvalidOperationException($"git {arguments} failed in {workingDirectoryPath}: {commandResult.StandardError}");
        }

        return commandResult.StandardOutput;
    }

    private static async Task<GitCommandResult> RunGitAllowFailureAsync(string workingDirectoryPath, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new GitCommandResult(process.ExitCode, standardOutput, standardError);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
