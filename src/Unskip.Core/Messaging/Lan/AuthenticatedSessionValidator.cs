namespace Unskip.Core.Messaging.Lan;

public static class AuthenticatedSessionValidator
{
    private const int MaximumIdentityLength = 256;

    public static AuthenticatedSessionValidation Validate(AuthenticatedSessionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.IsAuthenticated)
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.NotAuthenticated);
        }

        if (!context.IsMutuallyAuthenticated)
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.NotMutuallyAuthenticated);
        }

        if (!context.IsEncrypted)
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.NotEncrypted);
        }

        if (!context.IsSigned)
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.NotSigned);
        }

        var identity = context.RemoteIdentity?.Trim();
        if (string.IsNullOrWhiteSpace(identity)
            || identity.Length > MaximumIdentityLength
            || identity.Any(char.IsControl))
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.InvalidRemoteIdentity);
        }

        return AuthenticatedSessionValidation.Success(new AuthenticatedSender(identity));
    }
}
