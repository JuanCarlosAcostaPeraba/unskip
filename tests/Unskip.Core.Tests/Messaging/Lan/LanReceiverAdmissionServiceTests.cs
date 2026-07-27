using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class LanReceiverAdmissionServiceTests
{
    private readonly MutableClock _clock = new(LanProtocolTestData.Now);

    [Fact]
    public void ProtectedUniqueRequestIsAcceptedOnlyForLocalHandling()
    {
        var result = CreateService().Evaluate(
            LanProtocolTestData.CreateSession(),
            LanProtocolTestData.CreateRequest());

        Assert.True(result.IsAcceptedForLocalHandling);
        Assert.Equal(@"EXAMPLE\operator", result.Sender!.Identity);
        Assert.Equal(LanReceiverStatus.AcceptedForLocalHandling, result.Response.Status);
    }

    [Fact]
    public void UnauthenticatedSessionIsRejectedBeforeAdmission()
    {
        var result = CreateService().Evaluate(
            LanProtocolTestData.CreateSession() with { IsAuthenticated = false },
            LanProtocolTestData.CreateRequest());

        Assert.False(result.IsAcceptedForLocalHandling);
        Assert.Null(result.Sender);
        Assert.Equal(LanReceiverResponseCode.AuthenticationRequired, result.Response.Code);
    }

    [Fact]
    public void RepeatedMessageAndNonceAreRejectedAsReplay()
    {
        var service = CreateService();
        var request = LanProtocolTestData.CreateRequest();

        var first = service.Evaluate(LanProtocolTestData.CreateSession(), request);
        var replay = service.Evaluate(LanProtocolTestData.CreateSession("example\\OPERATOR"), request);

        Assert.True(first.IsAcceptedForLocalHandling);
        Assert.Equal(LanReceiverResponseCode.ReplayDetected, replay.Response.Code);
    }

    [Fact]
    public void ReusedMessageIdWithNewNonceIsRejectedAsReplay()
    {
        var service = CreateService();
        var request = LanProtocolTestData.CreateRequest();
        service.Evaluate(LanProtocolTestData.CreateSession(), request);

        var replay = service.Evaluate(
            LanProtocolTestData.CreateSession(),
            request with { Nonce = CreateNonce(90) });

        Assert.Equal(LanReceiverResponseCode.ReplayDetected, replay.Response.Code);
    }

    [Fact]
    public void ReusedNonceWithNewMessageIdIsRejectedAsReplay()
    {
        var service = CreateService();
        var request = LanProtocolTestData.CreateRequest();
        service.Evaluate(LanProtocolTestData.CreateSession(), request);

        var replay = service.Evaluate(
            LanProtocolTestData.CreateSession(),
            request with { MessageId = Guid.NewGuid() });

        Assert.Equal(LanReceiverResponseCode.ReplayDetected, replay.Response.Code);
    }

    [Fact]
    public void ReplayEntryExpiresAfterBoundedWindow()
    {
        var service = CreateService();
        var request = LanProtocolTestData.CreateRequest();
        service.Evaluate(LanProtocolTestData.CreateSession(), request);
        _clock.UtcNow += LanProtocolPolicy.ReplayWindow + TimeSpan.FromSeconds(1);
        var refreshedRequest = request with
        {
            SentAtUtc = _clock.UtcNow,
            ExpiresAtUtc = _clock.UtcNow.AddMinutes(1),
        };

        var result = service.Evaluate(LanProtocolTestData.CreateSession(), refreshedRequest);

        Assert.True(result.IsAcceptedForLocalHandling);
    }

    [Fact]
    public void PerIdentityRateLimitFailsClosedThenResets()
    {
        var service = CreateService();
        for (var index = 0; index < LanProtocolPolicy.MaximumRequestsPerIdentity; index++)
        {
            var accepted = service.Evaluate(
                LanProtocolTestData.CreateSession(),
                LanProtocolTestData.CreateRequest(
                    Guid.NewGuid(),
                    CreateNonce(index)));
            Assert.True(accepted.IsAcceptedForLocalHandling);
        }

        var limited = service.Evaluate(
            LanProtocolTestData.CreateSession(),
            LanProtocolTestData.CreateRequest(Guid.NewGuid(), CreateNonce(100)));

        Assert.Equal(LanReceiverStatus.RateLimited, limited.Response.Status);
        Assert.Equal(LanReceiverResponseCode.RateLimitExceeded, limited.Response.Code);

        _clock.UtcNow += LanProtocolPolicy.RateLimitWindow + TimeSpan.FromSeconds(1);
        var acceptedAfterReset = service.Evaluate(
            LanProtocolTestData.CreateSession(),
            LanProtocolTestData.CreateRequest(Guid.NewGuid(), CreateNonce(101)) with
            {
                SentAtUtc = _clock.UtcNow,
                ExpiresAtUtc = _clock.UtcNow.AddMinutes(1),
            });

        Assert.True(acceptedAfterReset.IsAcceptedForLocalHandling);
    }

    [Fact]
    public void InvalidVersionProducesHonestUnsupportedResponse()
    {
        var request = LanProtocolTestData.CreateRequest() with { Version = 9 };

        var result = CreateService().Evaluate(LanProtocolTestData.CreateSession(), request);

        Assert.False(result.IsAcceptedForLocalHandling);
        Assert.Equal(LanReceiverStatus.UnsupportedVersion, result.Response.Status);
        Assert.Equal(LanReceiverResponseCode.UnsupportedVersion, result.Response.Code);
    }

    private LanReceiverAdmissionService CreateService()
    {
        return new(
            new LanMessageRequestValidator(_clock),
            new IdentityRateLimiter(_clock),
            new ReplayProtectionService(_clock));
    }

    private static string CreateNonce(int seed)
    {
        return Convert.ToBase64String(
            Enumerable.Range(seed, LanProtocolPolicy.NonceByteLength)
                .Select(value => unchecked((byte)value))
                .ToArray());
    }
}
