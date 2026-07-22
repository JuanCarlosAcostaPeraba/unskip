using Unskip.Core.Networking;

namespace Unskip.Core.Messaging;

public static class MessageRequestValidator
{
    public static MessageValidationResult Validate(MessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationError>();
        var normalizedTarget = ValidateTarget(request.Target, errors);
        ValidateMessage(request.Message, errors);

        if (errors.Count > 0)
        {
            return MessageValidationResult.Failure(errors);
        }

        return MessageValidationResult.Success(
            new ValidatedMessageRequest(
                new MessageTarget(normalizedTarget!, MessageTargetKind.Hostname),
                request.Message));
    }

    private static string? ValidateTarget(string? target, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            errors.Add(new ValidationError("Target", ValidationErrorCode.Required, "Enter a computer name."));
            return null;
        }

        var normalizedTarget = target.Trim();
        if (NetworkAddressValidator.IsIpv4Address(normalizedTarget))
        {
            errors.Add(
                new ValidationError(
                    "Target",
                    ValidationErrorCode.Ipv4NotSupported,
                    "IPv4 destinations are not enabled because msg.exe documents /SERVER as a server name. Use the Windows computer name."));
            return null;
        }

        if (!NetworkAddressValidator.TryNormalizeHostname(normalizedTarget, out var hostname))
        {
            errors.Add(
                new ValidationError(
                    "Target",
                    ValidationErrorCode.InvalidHostname,
                    "Use a Windows computer name or DNS name containing only letters, digits, hyphens, and dots."));
            return null;
        }

        return hostname;
    }

    private static void ValidateMessage(string? message, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            errors.Add(new ValidationError("Message", ValidationErrorCode.Required, "Enter a message."));
            return;
        }

        if (message.Length > MessagePolicy.MaximumMessageLength)
        {
            errors.Add(
                new ValidationError(
                    "Message",
                    ValidationErrorCode.TooLong,
                    $"Messages can contain at most {MessagePolicy.MaximumMessageLength} characters."));
        }

        if (message.Any(IsUnsupportedControlCharacter))
        {
            errors.Add(
                new ValidationError(
                    "Message",
                    ValidationErrorCode.UnsupportedControlCharacter,
                    "The message contains an unsupported control character."));
        }
    }

    private static bool IsUnsupportedControlCharacter(char value)
    {
        return char.IsControl(value) && value is not '\r' and not '\n' and not '\t';
    }
}
