using System.Text.Json.Serialization;

namespace Unskip.Core.Messaging.Lan;

public sealed record LanReceiverResponse(
    [property: JsonRequired, JsonPropertyOrder(0)] int Version,
    [property: JsonRequired, JsonPropertyOrder(1)] Guid MessageId,
    [property: JsonRequired, JsonPropertyOrder(2)] LanReceiverStatus Status,
    [property: JsonRequired, JsonPropertyOrder(3)] LanReceiverResponseCode Code)
{
    public static LanReceiverResponse Accepted(Guid messageId)
    {
        return new(
            LanProtocolPolicy.CurrentVersion,
            messageId,
            LanReceiverStatus.AcceptedForLocalHandling,
            LanReceiverResponseCode.Accepted);
    }

    public static LanReceiverResponse Rejected(Guid messageId, LanReceiverResponseCode code)
    {
        var status = code switch
        {
            LanReceiverResponseCode.RateLimitExceeded => LanReceiverStatus.RateLimited,
            LanReceiverResponseCode.UnsupportedVersion => LanReceiverStatus.UnsupportedVersion,
            _ => LanReceiverStatus.Rejected,
        };

        return new(LanProtocolPolicy.CurrentVersion, messageId, status, code);
    }
}
