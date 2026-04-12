namespace Prompt.Tests.Integration;

internal static class GitIntegrationTestCollections
{
    public const string Serial = nameof(Serial);
}

[CollectionDefinition(GitIntegrationTestCollections.Serial, DisableParallelization = true)]
public sealed class GitStatusSerialCollection;
