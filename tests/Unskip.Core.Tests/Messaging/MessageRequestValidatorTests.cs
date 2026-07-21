using Unskip.Core.Messaging;

namespace Unskip.Core.Tests.Messaging;

public sealed class MessageRequestValidatorTests
{
    [Theory]
    [InlineData("desktop-01", "desktop-01")]
    [InlineData("FRONTDESK", "frontdesk")]
    [InlineData("host.example.test", "host.example.test")]
    public void ValidHostnameProducesNormalizedRequest(string target, string expectedTarget)
    {
        var result = MessageRequestValidator.Validate(new MessageRequest(target, "Maintenance starts in ten minutes."));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Request);
        Assert.Equal(expectedTarget, result.Request.Target.Value);
        Assert.Equal(MessageTargetKind.Hostname, result.Request.Target.Kind);
    }

    [Theory]
    [InlineData("-server")]
    [InlineData("server-")]
    [InlineData("server_name")]
    [InlineData("server name")]
    [InlineData("server&whoami")]
    [InlineData("server/example")]
    [InlineData("servidór")]
    public void InvalidHostnameIsRejected(string target)
    {
        var result = MessageRequestValidator.Validate(new MessageRequest(target, "Test message"));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidHostname, error.Code);
    }

    [Fact]
    public void CanonicalIpv4AddressIsRejectedWithSpecificExplanation()
    {
        var result = MessageRequestValidator.Validate(new MessageRequest("192.0.2.25", "Test message"));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.Ipv4NotSupported, error.Code);
        Assert.Contains("computer name", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlternateIpv4NotationIsAlsoRejected()
    {
        var result = MessageRequestValidator.Validate(new MessageRequest("127.1", "Test message"));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.Ipv4NotSupported, error.Code);
    }

    [Fact]
    public void ShellCharactersRemainMessageData()
    {
        const string message = "Quoted \"text\" & | < > ^ %PATH% $(ignored)";

        var result = MessageRequestValidator.Validate(new MessageRequest("desktop-01", message));

        Assert.True(result.IsValid);
        Assert.Equal(message, result.Request!.Message);
    }

    [Fact]
    public void EmptyMessageIsRejected()
    {
        var result = MessageRequestValidator.Validate(new MessageRequest("desktop-01", " \t "));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.Required, error.Code);
    }

    [Fact]
    public void OversizedMessageIsRejected()
    {
        var message = new string('a', MessagePolicy.MaximumMessageLength + 1);

        var result = MessageRequestValidator.Validate(new MessageRequest("desktop-01", message));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.TooLong, error.Code);
    }

    [Fact]
    public void NullCharacterIsRejected()
    {
        var result = MessageRequestValidator.Validate(new MessageRequest("desktop-01", "before\0after"));

        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.UnsupportedControlCharacter, error.Code);
    }
}
