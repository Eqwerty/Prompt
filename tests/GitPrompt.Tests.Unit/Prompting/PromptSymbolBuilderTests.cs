using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Constants;
using GitPrompt.Prompting;

namespace GitPrompt.Tests.Unit.Prompting;

[Collection(ConfigIsolationCollection.Name)]
public sealed class PromptSymbolBuilderTests
{
    [Fact]
    public void Build_WhenOnWindows_ShouldReturnGreaterThanRegardlessOfUser()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config());
        var platformProvider = new TestPlatformProvider(isWindows: true);

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(PromptSymbols.Windows);
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsRoot_ShouldReturnHash()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config());
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "root");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(PromptSymbols.UnixRoot);
    }

    [Fact]
    public void Build_WhenOnUnixAndUserIsNotRoot_ShouldReturnDollar()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config());
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "me");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be(PromptSymbols.Unix);
    }

    [Fact]
    public void Build_WhenCustomSymbolIsConfigured_ShouldReturnCustomSymbolRegardlessOfPlatform()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Layout = new Config.LayoutConfig { Symbol = "❯" } });
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "me");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be("❯");
    }

    [Fact]
    public void Build_WhenCustomSymbolIsConfiguredOnWindows_ShouldReturnCustomSymbol()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Layout = new Config.LayoutConfig { Symbol = "λ" } });
        var platformProvider = new TestPlatformProvider(isWindows: true);

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().Be("λ");
    }

    [Fact]
    public void Build_WhenCustomSymbolIsEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        using var _ = ConfigReader.OverrideForTesting(new Config { Layout = new Config.LayoutConfig { Symbol = "" } });
        var platformProvider = new TestPlatformProvider(isWindows: false, user: "me");

        // Act
        var symbol = PromptSymbolBuilder.Build(platformProvider);

        // Assert
        symbol.Should().BeEmpty();
    }
}
