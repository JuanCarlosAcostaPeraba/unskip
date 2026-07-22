namespace Unskip.Core.Devices;

public enum DeviceValidationErrorCode
{
    Required,
    TooLong,
    InvalidCharacters,
    InvalidHostname,
    InvalidIpv4,
    DestinationRequired,
    PreferredDestinationUnavailable,
}
