using Unskip.Core.Messaging.Lan;

namespace Unskip.Core.Tests.Messaging.Lan;

public sealed class AuthenticatedSessionValidatorTests
{
    [Fact]
    public void FullyProtectedSessionProducesAuthoritativeSender()
    {
        var result = AuthenticatedSessionValidator.Validate(
            LanProtocolTestData.CreateSession("  EXAMPLE\\operator  "));

        Assert.True(result.IsValid);
        Assert.Equal("windows-sid:S-1-5-21-1000", result.Sender!.IdentityKey);
        Assert.Equal(@"EXAMPLE\operator", result.Sender.DisplayName);
        Assert.Equal(AuthenticationScheme.WindowsNegotiate, result.Sender.Scheme);
    }

    [Theory]
    [MemberData(nameof(UnprotectedSessions))]
    public void AnyMissingSecurityPropertyFailsClosed(
        AuthenticatedSessionContext session,
        AuthenticatedSessionValidationError expectedError)
    {
        var result = AuthenticatedSessionValidator.Validate(session);

        Assert.False(result.IsValid);
        Assert.Null(result.Sender);
        Assert.Equal(expectedError, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EXAMPLE\\user\nadmin")]
    public void InvalidDisplayNameIsRejected(string? displayName)
    {
        var result = AuthenticatedSessionValidator.Validate(
            LanProtocolTestData.CreateSession(displayName!));

        Assert.Equal(AuthenticatedSessionValidationError.InvalidDisplayName, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("windows-sid:not-a-sid")]
    [InlineData("mtls-sha256:0000")]
    public void InvalidAuthoritativeIdentityKeyIsRejected(string? identityKey)
    {
        var result = AuthenticatedSessionValidator.Validate(
            LanProtocolTestData.CreateSession(identityKey: identityKey!));

        Assert.Equal(AuthenticatedSessionValidationError.InvalidIdentityKey, result.Error);
    }

    public static TheoryData<AuthenticatedSessionContext, AuthenticatedSessionValidationError>
        UnprotectedSessions =>
        new()
        {
            {
                new(
                    false,
                    true,
                    true,
                    true,
                    AuthenticationScheme.WindowsNegotiate,
                    "windows-sid:S-1-5-21-1000",
                    @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotAuthenticated
            },
            {
                new(
                    true,
                    false,
                    true,
                    true,
                    AuthenticationScheme.WindowsNegotiate,
                    "windows-sid:S-1-5-21-1000",
                    @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotMutuallyAuthenticated
            },
            {
                new(
                    true,
                    true,
                    false,
                    true,
                    AuthenticationScheme.WindowsNegotiate,
                    "windows-sid:S-1-5-21-1000",
                    @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotEncrypted
            },
            {
                new(
                    true,
                    true,
                    true,
                    false,
                    AuthenticationScheme.WindowsNegotiate,
                    "windows-sid:S-1-5-21-1000",
                    @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotSigned
            },
        };
}
