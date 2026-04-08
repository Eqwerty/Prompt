using FluentAssertions;

namespace Prompt.Tests.Unit;

public sealed class ProgramTests
{
    [Fact]
    public void GetPromptSymbol_WhenOnWindows_ShouldReturnGreaterThan()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: true, user: "root"));

        symbol.Should().Be(">");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, user: "root"));

        symbol.Should().Be("#");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, user: "me"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndWindowsUsernameIsRoot_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false, windowsUserName: "root"));

        symbol.Should().Be("$");
    }

    [Fact]
    public void GetPromptSymbol_WhenOnUnixAndNoUserSet_ShouldReturnDollar()
    {
        var symbol = Program.GetPromptSymbol(new TestPlatformProvider(isWindows: false));

        symbol.Should().Be("$");
    }
}
