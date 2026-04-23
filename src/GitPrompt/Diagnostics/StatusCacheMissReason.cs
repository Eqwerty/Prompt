namespace GitPrompt.Diagnostics;

internal enum StatusCacheMissReason
{
    Disabled,
    NoEntry,
    ParseError,
    TtlExpired,
    FingerprintChanged,
    InvalidationToken
}
