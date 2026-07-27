using Unskip.Core.Messaging.Lan;
using Unskip.Core.Time;

namespace Unskip.Core.Tests.Messaging.Lan;

internal static class LanProtocolTestData
{
    internal static readonly DateTimeOffset Now =
        new(2026, 7, 27, 9, 30, 0, TimeSpan.Zero);

    internal static LanMessageRequest CreateRequest(
        Guid? messageId = null,
        string? nonce = null,
        string message = "A maintenance window starts in ten minutes.")
    {
        return new(
            LanProtocolPolicy.CurrentVersion,
            messageId ?? Guid.Parse("bff8f9ef-cc1e-4f76-b028-77903ae39787"),
            Now,
            Now.AddMinutes(1),
            nonce ?? Convert.ToBase64String(Enumerable.Range(1, 16).Select(value => (byte)value).ToArray()),
            LanMessageKind.UrgentAttention,
            message);
    }

    internal static AuthenticatedSessionContext CreateSession(string identity = @"EXAMPLE\operator")
    {
        return new(true, true, true, true, identity);
    }
}

internal sealed class MutableClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
