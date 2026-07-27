namespace Unskip.Core.Messaging.Lan;

public sealed record LanRequestValidation(bool IsValid, LanRequestValidationError Error)
{
    public static LanRequestValidation Success { get; } = new(true, LanRequestValidationError.None);

    public static LanRequestValidation Failure(LanRequestValidationError error)
    {
        return new(false, error);
    }
}

public enum LanRequestValidationError
{
    None,
    UnsupportedVersion,
    MissingMessageId,
    InvalidTimestamp,
    Expired,
    InvalidLifetime,
    InvalidNonce,
    UnsupportedKind,
    InvalidMessage,
}
