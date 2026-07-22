namespace Unskip.Core.Messaging;

/// <summary>
/// Defines user-input limits enforced before native delivery.
/// </summary>
public static class MessagePolicy
{
    public const int MaximumMessageLength = 1_024;

    public const int MaximumHostnameLength = Networking.NetworkAddressValidator.MaximumHostnameLength;
}
