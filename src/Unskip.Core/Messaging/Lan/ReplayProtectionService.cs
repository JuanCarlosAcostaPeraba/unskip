using Unskip.Core.Time;

namespace Unskip.Core.Messaging.Lan;

public sealed class ReplayProtectionService
{
    private readonly IClock _clock;
    private readonly Dictionary<ReplayMessageKey, DateTimeOffset> _messageIds =
        new(ReplayMessageKeyComparer.Instance);
    private readonly Dictionary<ReplayNonceKey, DateTimeOffset> _nonces =
        new(ReplayNonceKeyComparer.Instance);
    private readonly Lock _gate = new();

    public ReplayProtectionService(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ReplayProtectionResult TryAccept(AuthenticatedSender sender, LanMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            var now = _clock.UtcNow;
            RemoveExpiredEntries(now);

            var messageKey = new ReplayMessageKey(sender.Identity, request.MessageId);
            var nonceKey = new ReplayNonceKey(sender.Identity, request.Nonce);
            if (_messageIds.ContainsKey(messageKey) || _nonces.ContainsKey(nonceKey))
            {
                return ReplayProtectionResult.ReplayDetected;
            }

            if (_messageIds.Count >= LanProtocolPolicy.ReplayCapacity)
            {
                return ReplayProtectionResult.CapacityExceeded;
            }

            var expiresAt = now + LanProtocolPolicy.ReplayWindow;
            _messageIds.Add(messageKey, expiresAt);
            _nonces.Add(nonceKey, expiresAt);
            return ReplayProtectionResult.Accepted;
        }
    }

    private void RemoveExpiredEntries(DateTimeOffset now)
    {
        foreach (var entry in _messageIds.Where(entry => entry.Value <= now).ToList())
        {
            _messageIds.Remove(entry.Key);
        }

        foreach (var entry in _nonces.Where(entry => entry.Value <= now).ToList())
        {
            _nonces.Remove(entry.Key);
        }
    }

    private sealed record ReplayMessageKey(string Identity, Guid MessageId);

    private sealed record ReplayNonceKey(string Identity, string Nonce);

    private sealed class ReplayMessageKeyComparer : IEqualityComparer<ReplayMessageKey>
    {
        public static ReplayMessageKeyComparer Instance { get; } = new();

        public bool Equals(ReplayMessageKey? left, ReplayMessageKey? right)
        {
            return ReferenceEquals(left, right)
                || (left is not null
                    && right is not null
                    && StringComparer.OrdinalIgnoreCase.Equals(left.Identity, right.Identity)
                    && left.MessageId == right.MessageId);
        }

        public int GetHashCode(ReplayMessageKey value)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Identity),
                value.MessageId);
        }
    }

    private sealed class ReplayNonceKeyComparer : IEqualityComparer<ReplayNonceKey>
    {
        public static ReplayNonceKeyComparer Instance { get; } = new();

        public bool Equals(ReplayNonceKey? left, ReplayNonceKey? right)
        {
            return ReferenceEquals(left, right)
                || (left is not null
                    && right is not null
                    && StringComparer.OrdinalIgnoreCase.Equals(left.Identity, right.Identity)
                    && StringComparer.Ordinal.Equals(left.Nonce, right.Nonce));
        }

        public int GetHashCode(ReplayNonceKey value)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Identity),
                StringComparer.Ordinal.GetHashCode(value.Nonce));
        }
    }
}

public enum ReplayProtectionResult
{
    Accepted,
    ReplayDetected,
    CapacityExceeded,
}
