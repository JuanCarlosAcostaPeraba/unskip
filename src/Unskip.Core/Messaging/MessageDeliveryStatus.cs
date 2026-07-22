namespace Unskip.Core.Messaging;

public enum MessageDeliveryStatus
{
    Sending,
    Sent,
    Rejected,
    TimedOut,
    Cancelled,
    Failed,
}
