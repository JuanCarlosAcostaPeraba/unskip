namespace Unskip.Core.Messaging;

public enum ValidationErrorCode
{
    Required,
    TooLong,
    InvalidHostname,
    Ipv4NotSupported,
    UnsupportedControlCharacter,
}
