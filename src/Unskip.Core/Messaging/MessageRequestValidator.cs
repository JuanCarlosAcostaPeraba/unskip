using Unskip.Core.Networking;

namespace Unskip.Core.Messaging;

public static class MessageRequestValidator
{
    public static MessageValidationResult Validate(MessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationError>();
        var validatedTarget = ValidateTarget(request.Target, errors);
        ValidateMessage(request.Message, errors);

        if (errors.Count > 0)
        {
            return MessageValidationResult.Failure(errors);
        }

        return MessageValidationResult.Success(
            new ValidatedMessageRequest(
                validatedTarget!,
                request.Message));
    }

    private static MessageTarget? ValidateTarget(string? target, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            errors.Add(new ValidationError("Target", ValidationErrorCode.Required, "Enter a computer name or IPv4 address."));
            return null;
        }

        var normalizedTarget = target.Trim();
        if (NetworkAddressValidator.TryNormalizeCanonicalIpv4(normalizedTarget, out var ipv4Address))
        {
            return new MessageTarget(ipv4Address!, MessageTargetKind.Ipv4Address);
        }

        if (NetworkAddressValidator.IsIpv4Address(normalizedTarget))
        {
            errors.Add(
                new ValidationError(
                    "Target",
                    ValidationErrorCode.InvalidIpv4Address,
                    "Use a canonical IPv4 address with four dotted-decimal segments and no leading zeroes."));
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

        return new MessageTarget(hostname!, MessageTargetKind.Hostname);
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
