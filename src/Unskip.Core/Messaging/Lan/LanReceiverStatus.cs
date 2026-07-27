namespace Unskip.Core.Messaging.Lan;

public enum LanReceiverStatus
{
    AcceptedForLocalHandling,
    Rejected,
    RateLimited,
    UnsupportedVersion,
}
