namespace Unskip.Core.Messaging.Lan;

public static class AuthenticatedSessionValidator
{
    private const int MaximumDisplayNameLength = 256;

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

        if (!AuthenticatedIdentityKey.TryNormalize(
                context.Scheme,
                context.RemoteIdentityKey,
                out var identityKey))
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.InvalidIdentityKey);
        }

        var displayName = context.RemoteDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName)
            || displayName.Length > MaximumDisplayNameLength
            || displayName.Any(char.IsControl))
        {
            return AuthenticatedSessionValidation.Failure(
                AuthenticatedSessionValidationError.InvalidDisplayName);
        }

        return AuthenticatedSessionValidation.Success(
            new AuthenticatedSender(identityKey!, displayName, context.Scheme));
    }
}
