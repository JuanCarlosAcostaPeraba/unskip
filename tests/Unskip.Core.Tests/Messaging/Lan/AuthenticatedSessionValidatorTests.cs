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
        Assert.Equal(@"EXAMPLE\operator", result.Sender!.Identity);
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
    public void InvalidRemoteIdentityIsRejected(string? identity)
    {
        var result = AuthenticatedSessionValidator.Validate(
            LanProtocolTestData.CreateSession(identity!));

        Assert.Equal(AuthenticatedSessionValidationError.InvalidRemoteIdentity, result.Error);
    }

    public static TheoryData<AuthenticatedSessionContext, AuthenticatedSessionValidationError>
        UnprotectedSessions =>
        new()
        {
            {
                new(false, true, true, true, @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotAuthenticated
            },
            {
                new(true, false, true, true, @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotMutuallyAuthenticated
            },
            {
                new(true, true, false, true, @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotEncrypted
            },
            {
                new(true, true, true, false, @"EXAMPLE\operator"),
                AuthenticatedSessionValidationError.NotSigned
            },
        };
}
