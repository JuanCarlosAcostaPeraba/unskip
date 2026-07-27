using Unskip.Core.Time;

namespace Unskip.Core.Messaging.Lan;

public sealed class LanMessageRequestValidator(IClock clock)
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public LanRequestValidation Validate(LanMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Version != LanProtocolPolicy.CurrentVersion)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.UnsupportedVersion);
        }

        if (request.MessageId == Guid.Empty)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.MissingMessageId);
        }

        if (request.SentAtUtc.Offset != TimeSpan.Zero || request.ExpiresAtUtc.Offset != TimeSpan.Zero)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.InvalidTimestamp);
        }

        var now = _clock.UtcNow;
        if (request.SentAtUtc > now + LanProtocolPolicy.MaximumClockSkew)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.InvalidTimestamp);
        }

        if (request.ExpiresAtUtc <= now)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.Expired);
        }

        var lifetime = request.ExpiresAtUtc - request.SentAtUtc;
        if (lifetime <= TimeSpan.Zero || lifetime > LanProtocolPolicy.MaximumMessageLifetime)
        {
            return LanRequestValidation.Failure(LanRequestValidationError.InvalidLifetime);
        }

        if (!HasValidNonce(request.Nonce))
        {
            return LanRequestValidation.Failure(LanRequestValidationError.InvalidNonce);
        }

        if (!Enum.IsDefined(request.Kind))
        {
            return LanRequestValidation.Failure(LanRequestValidationError.UnsupportedKind);
        }

        if (!HasValidMessage(request.Message))
        {
            return LanRequestValidation.Failure(LanRequestValidationError.InvalidMessage);
        }

        return LanRequestValidation.Success;
    }

    private static bool HasValidNonce(string? nonce)
    {
        if (string.IsNullOrWhiteSpace(nonce))
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[LanProtocolPolicy.NonceByteLength];
        if (!Convert.TryFromBase64String(nonce, bytes, out var bytesWritten)
            || bytesWritten != LanProtocolPolicy.NonceByteLength)
        {
            return false;
        }

        return string.Equals(
            nonce,
            Convert.ToBase64String(bytes),
            StringComparison.Ordinal);
    }

    private static bool HasValidMessage(string? message)
    {
        return !string.IsNullOrWhiteSpace(message)
            && message.Length <= MessagePolicy.MaximumMessageLength
            && !message.Any(character =>
                char.IsControl(character) && character is not '\r' and not '\n' and not '\t');
    }
}
