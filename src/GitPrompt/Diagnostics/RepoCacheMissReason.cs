namespace GitPrompt.Diagnostics;

internal enum RepoCacheMissReason
{
    Disabled,
    NoEntry,
    ParseError,
    TtlExpired
}
