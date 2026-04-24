namespace GitPrompt.Configuration;

internal sealed record ConfigLoadResult(string FilePath, ConfigLoadStatus Status, Config Config);
