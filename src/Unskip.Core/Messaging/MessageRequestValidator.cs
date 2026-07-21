using System.Net;
using System.Net.Sockets;

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
        if (IsIpv4Address(normalizedTarget))
        {
            errors.Add(
                new ValidationError(
                    "Target",
                    ValidationErrorCode.Ipv4NotSupported,
                    "IPv4 destinations are not enabled because msg.exe documents /SERVER as a server name. Use the Windows computer name."));
            return null;
        }

        if (!IsValidHostname(normalizedTarget))
        {
            errors.Add(
                new ValidationError(
                    "Target",
                    ValidationErrorCode.InvalidHostname,
                    "Use a Windows computer name or DNS name containing only letters, digits, hyphens, and dots."));
            return null;
        }

        return normalizedTarget.ToLowerInvariant();
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

    private static bool IsIpv4Address(string value)
    {
        return IPAddress.TryParse(value, out var address)
            && address.AddressFamily == AddressFamily.InterNetwork;
    }

    private static bool IsValidHostname(string value)
    {
        if (value.Length is 0 or > MessagePolicy.MaximumHostnameLength
            || value[0] == '.'
            || value[^1] == '.')
        {
            return false;
        }

        foreach (var label in value.Split('.'))
        {
            if (label.Length is 0 or > 63
                || !IsAsciiLetterOrDigit(label[0])
                || !IsAsciiLetterOrDigit(label[^1])
                || label.Any(character => !IsAsciiLetterOrDigit(character) && character != '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return value is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9';
    }

    private static bool IsUnsupportedControlCharacter(char value)
    {
        return char.IsControl(value) && value is not '\r' and not '\n' and not '\t';
    }
}
