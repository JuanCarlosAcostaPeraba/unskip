namespace Unskip.Core.Messaging.Lan;

public sealed record AuthenticatedSessionContext(
    bool IsAuthenticated,
    bool IsMutuallyAuthenticated,
    bool IsEncrypted,
    bool IsSigned,
    AuthenticationScheme Scheme,
    string? RemoteIdentityKey,
    string? RemoteDisplayName);

public sealed record AuthenticatedSender(
    string IdentityKey,
    string DisplayName,
    AuthenticationScheme Scheme);

public sealed record AuthenticatedSessionValidation(
    bool IsValid,
    AuthenticatedSender? Sender,
    AuthenticatedSessionValidationError Error)
{
    public static AuthenticatedSessionValidation Success(AuthenticatedSender sender)
    {
        return new(true, sender, AuthenticatedSessionValidationError.None);
    }

    public static AuthenticatedSessionValidation Failure(AuthenticatedSessionValidationError error)
    {
        return new(false, null, error);
    }
}

public enum AuthenticatedSessionValidationError
{
    None,
    NotAuthenticated,
    NotMutuallyAuthenticated,
    NotEncrypted,
    NotSigned,
    InvalidIdentityKey,
    InvalidDisplayName,
}

public enum AuthenticationScheme
{
    WindowsNegotiate,
    MutualTls,
}
