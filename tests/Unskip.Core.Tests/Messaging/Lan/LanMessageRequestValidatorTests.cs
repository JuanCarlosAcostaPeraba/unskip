using Unskip.Core.Messaging;
using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class LanMessageRequestValidatorTests
{
    private readonly MutableClock _clock = new(LanProtocolTestData.Now);

    [Fact]
    public void ValidVersionOneRequestIsAccepted()
    {
        var result = CreateValidator().Validate(LanProtocolTestData.CreateRequest());

        Assert.True(result.IsValid);
        Assert.Equal(LanRequestValidationError.None, result.Error);
    }

    [Fact]
    public void RequestPayloadHasNoSenderIdentityProperty()
    {
        var propertyNames = typeof(LanMessageRequest)
            .GetProperties()
            .Select(property => property.Name);

        Assert.DoesNotContain(
            propertyNames,
            name => name.Contains("sender", StringComparison.OrdinalIgnoreCase)
                || name.Contains("identity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnsupportedVersionIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with { Version = 2 };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.UnsupportedVersion, result.Error);
    }

    [Fact]
    public void EmptyMessageIdIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with { MessageId = Guid.Empty };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.MissingMessageId, result.Error);
    }

    [Fact]
    public void NonUtcTimestampIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with
        {
            SentAtUtc = LanProtocolTestData.Now.ToOffset(TimeSpan.FromHours(1)),
        };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidTimestamp, result.Error);
    }

    [Fact]
    public void TimestampBeyondClockSkewIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with
        {
            SentAtUtc = LanProtocolTestData.Now + LanProtocolPolicy.MaximumClockSkew + TimeSpan.FromSeconds(1),
            ExpiresAtUtc = LanProtocolTestData.Now + LanProtocolPolicy.MaximumClockSkew + TimeSpan.FromMinutes(1),
        };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidTimestamp, result.Error);
    }

    [Fact]
    public void ExpiredRequestIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with
        {
            SentAtUtc = LanProtocolTestData.Now.AddMinutes(-1),
            ExpiresAtUtc = LanProtocolTestData.Now.AddTicks(-1),
        };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.Expired, result.Error);
    }

    [Fact]
    public void ExcessiveLifetimeIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with
        {
            ExpiresAtUtc = LanProtocolTestData.Now
                + LanProtocolPolicy.MaximumMessageLifetime
                + TimeSpan.FromSeconds(1),
        };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidLifetime, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("AQID")]
    [InlineData("AQIDBAUGBwgJCgsMDQ4PEA==\n")]
    public void InvalidNonceIsRejected(string nonce)
    {
        var request = LanProtocolTestData.CreateRequest() with { Nonce = nonce };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidNonce, result.Error);
    }

    [Fact]
    public void UnsupportedMessageKindIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest() with { Kind = (LanMessageKind)999 };

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.UnsupportedKind, result.Error);
    }

    [Fact]
    public void OversizedMessageIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest(
            message: new string('a', MessagePolicy.MaximumMessageLength + 1));

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidMessage, result.Error);
    }

    [Fact]
    public void UnsupportedControlCharacterIsRejected()
    {
        var request = LanProtocolTestData.CreateRequest(message: "before\0after");

        var result = CreateValidator().Validate(request);

        Assert.Equal(LanRequestValidationError.InvalidMessage, result.Error);
    }

    private LanMessageRequestValidator CreateValidator()
    {
        return new(_clock);
    }
}
