using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class ProtocolProtectionCapacityTests
{
    [Fact]
    public void ReplayCapacityFailsClosedAndRecoversAfterWindow()
    {
        var clock = new MutableClock(LanProtocolTestData.Now);
        var protection = new ReplayProtectionService(clock);
        var sender = new AuthenticatedSender(@"EXAMPLE\operator");

        for (var index = 0; index < LanProtocolPolicy.ReplayCapacity; index++)
        {
            var result = protection.TryAccept(sender, CreateRequest(index));
            Assert.Equal(ReplayProtectionResult.Accepted, result);
        }

        Assert.Equal(
            ReplayProtectionResult.CapacityExceeded,
            protection.TryAccept(sender, CreateRequest(LanProtocolPolicy.ReplayCapacity)));

        clock.UtcNow += LanProtocolPolicy.ReplayWindow + TimeSpan.FromSeconds(1);

        Assert.Equal(
            ReplayProtectionResult.Accepted,
            protection.TryAccept(sender, CreateRequest(LanProtocolPolicy.ReplayCapacity + 1)));
    }

    [Fact]
    public void IdentityCapacityFailsClosedAndRecoversAfterWindow()
    {
        var clock = new MutableClock(LanProtocolTestData.Now);
        var limiter = new IdentityRateLimiter(clock);

        for (var index = 0; index < LanProtocolPolicy.MaximumTrackedIdentities; index++)
        {
            var result = limiter.TryAcquire(new AuthenticatedSender($@"EXAMPLE\user-{index}"));
            Assert.Equal(IdentityRateLimitResult.Accepted, result);
        }

        Assert.Equal(
            IdentityRateLimitResult.CapacityExceeded,
            limiter.TryAcquire(new AuthenticatedSender(@"EXAMPLE\overflow")));

        clock.UtcNow += LanProtocolPolicy.RateLimitWindow + TimeSpan.FromSeconds(1);

        Assert.Equal(
            IdentityRateLimitResult.Accepted,
            limiter.TryAcquire(new AuthenticatedSender(@"EXAMPLE\after-window")));
    }

    private static LanMessageRequest CreateRequest(int seed)
    {
        var bytes = new byte[LanProtocolPolicy.NonceByteLength];
        BitConverter.GetBytes(seed).CopyTo(bytes, 0);
        return LanProtocolTestData.CreateRequest(
            CreateDeterministicGuid(seed),
            Convert.ToBase64String(bytes));
    }

    private static Guid CreateDeterministicGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(seed + 1).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
