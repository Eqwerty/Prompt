using GitPrompt.Configuration;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Prompting;

internal readonly record struct PromptResult(
    string ContextSegment,
    string CommandDurationSegment,
    string GitStatusSegment,
    string PromptSymbol,
    TimeSpan ContextElapsed,
    TimeSpan GitElapsed,
    TimeSpan TotalElapsed)
{
    internal string Output
    {
        get
        {
            var config = ConfigReader.Config;
            var promptSymbolSegment = $"{ColorPromptSymbol}{PromptSymbol}{ColorReset} ";

            var body = (config.Layout!.Multiline ?? true)
                ? $"{PromptLine}\n{promptSymbolSegment}"
                : $"{PromptLine} {promptSymbolSegment}";

            return (config.Layout!.NewlineBefore ?? false) ? $"\n{body}" : body;
        }
    }

    internal string PromptLine
    {
        get
        {
            var hasDuration = !string.IsNullOrEmpty(CommandDurationSegment);
            var hasGit = !string.IsNullOrEmpty(GitStatusSegment);

            if (hasDuration && hasGit)
            {
                return $"{ContextSegment} {CommandDurationSegment} {GitStatusSegment}";
            }

            if (hasDuration)
            {
                return $"{ContextSegment} {CommandDurationSegment}";
            }

            if (hasGit)
            {
                return $"{ContextSegment} {GitStatusSegment}";
            }

            return ContextSegment;
        }
    }
}

