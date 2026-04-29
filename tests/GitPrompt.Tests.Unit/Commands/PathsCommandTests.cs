using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class PathsCommandTests
{
    [Fact]
    public void BuildReport_ShouldContainAllLabels()
    {
        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: "/usr/local/bin/gitprompt",
            configPath: "/home/user/.config/gitprompt/config.jsonc",
            aliasesPath: "/home/user/.local/share/gitprompt/git_aliases.sh",
            cacheDirPath: "/home/user/.cache/gitprompt",
            shellConfigPath: "/home/user/.bashrc");

        // Assert
        report.Should().Contain("binary");
        report.Should().Contain("config");
        report.Should().Contain("aliases");
        report.Should().Contain("cache dir");
        report.Should().Contain("shell config");
    }

    [Fact]
    public void BuildReport_ShouldContainPathValues()
    {
        // Arrange
        var configPath = "/home/user/.config/gitprompt/config.jsonc";
        var aliasesPath = "/home/user/.local/share/gitprompt/git_aliases.sh";
        var cacheDirPath = "/home/user/.cache/gitprompt";

        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: "/usr/local/bin/gitprompt",
            configPath: configPath,
            aliasesPath: aliasesPath,
            cacheDirPath: cacheDirPath,
            shellConfigPath: "/home/user/.bashrc");

        // Assert
        report.Should().Contain(configPath);
        report.Should().Contain(aliasesPath);
        report.Should().Contain(cacheDirPath);
    }

    [Fact]
    public void BuildReport_WhenPathDoesNotExist_ShouldAppendNotFound()
    {
        // Arrange
        var missingPath = "/nonexistent/path/to/config.jsonc";

        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: null,
            configPath: missingPath,
            aliasesPath: "/nonexistent/aliases.sh",
            cacheDirPath: "/nonexistent/cache",
            shellConfigPath: null);

        // Assert
        report.Should().Contain($"{missingPath} (not found)");
    }

    [Fact]
    public void BuildReport_WhenShellConfigPathIsNull_ShouldShowNoneFound()
    {
        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: null,
            configPath: "/nonexistent/config.jsonc",
            aliasesPath: "/nonexistent/aliases.sh",
            cacheDirPath: "/nonexistent/cache",
            shellConfigPath: null);

        // Assert
        report.Should().Contain("(none found)");
    }

    [Fact]
    public void BuildReport_WhenBinaryPathIsNull_ShouldShowNoneFound()
    {
        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: null,
            configPath: "/nonexistent/config.jsonc",
            aliasesPath: "/nonexistent/aliases.sh",
            cacheDirPath: "/nonexistent/cache",
            shellConfigPath: null);

        // Assert
        report.Should().Contain("(none found)");
    }

    [Fact]
    public void BuildReport_ShouldRenderBoxWithTitle()
    {
        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: null,
            configPath: "/nonexistent/config.jsonc",
            aliasesPath: "/nonexistent/aliases.sh",
            cacheDirPath: "/nonexistent/cache",
            shellConfigPath: null);

        // Assert
        report.Should().Contain("GitPrompt paths");
        report.Should().Contain("╭");
        report.Should().Contain("╰");
    }

    [Fact]
    public void Run_ShouldWriteToProvidedOutput()
    {
        // Arrange
        var output = new StringWriter();

        // Act
        PathsCommand.Run(output);

        // Assert
        output.ToString().Should().NotBeEmpty();
        output.ToString().Should().Contain("binary");
        output.ToString().Should().Contain("config");
        output.ToString().Should().Contain("aliases");
        output.ToString().Should().Contain("cache dir");
        output.ToString().Should().Contain("shell config");
    }

    [Fact]
    public void BuildReport_ShouldNormalizeBackslashesToForwardSlashes()
    {
        // Arrange
        var windowsPath = @"C:\Users\user\.config\gitprompt\config.jsonc";

        // Act
        var report = PathsCommand.BuildReport(
            binaryPath: null,
            configPath: windowsPath,
            aliasesPath: @"C:\Users\user\.local\share\gitprompt\git_aliases.sh",
            cacheDirPath: @"C:\Users\user\.cache\gitprompt",
            shellConfigPath: null);

        // Assert
        report.Should().Contain("C:/Users/user/.config/gitprompt/config.jsonc");
        report.Should().NotContain(@"config\gitprompt\config.jsonc");
    }
}
