namespace Unskip.Core.Messaging.Lan;

public sealed record LanFrameReadResult<T>(
    LanFrameReadStatus Status,
    T? Value,
    LanRequestValidationError ValidationError)
    where T : class
{
    public bool IsSuccess => Status == LanFrameReadStatus.Success;
}

public enum LanFrameReadStatus
{
    Success,
    EndOfStream,
    InvalidLength,
    Truncated,
    MalformedPayload,
    UnsupportedVersion,
    InvalidPayload,
}
