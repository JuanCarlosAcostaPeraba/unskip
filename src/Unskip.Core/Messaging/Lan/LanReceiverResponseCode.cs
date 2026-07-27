namespace Unskip.Core.Messaging.Lan;

public enum LanReceiverResponseCode
{
    Accepted,
    AuthenticationRequired,
    InvalidRequest,
    Expired,
    ReplayDetected,
    ReplayCapacityExceeded,
    RateLimitExceeded,
    RateLimitCapacityExceeded,
    UnsupportedVersion,
}
