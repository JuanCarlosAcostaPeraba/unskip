namespace Unskip.Core.Messaging;

public enum ValidationErrorCode
{
    Required,
    TooLong,
    InvalidHostname,
    InvalidIpv4Address,
    UnsupportedControlCharacter,
}
