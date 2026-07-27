using Unskip.Core.Time;

namespace Unskip.Core.Messaging.Lan;

public sealed class IdentityRateLimiter
{
    private readonly IClock _clock;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, RateWindow> _windows =
        new(StringComparer.OrdinalIgnoreCase);

    public IdentityRateLimiter(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public IdentityRateLimitResult TryAcquire(AuthenticatedSender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        lock (_gate)
        {
            var now = _clock.UtcNow;
            RemoveExpiredWindows(now);

            if (!_windows.TryGetValue(sender.Identity, out var window))
            {
                if (_windows.Count >= LanProtocolPolicy.MaximumTrackedIdentities)
                {
                    return IdentityRateLimitResult.CapacityExceeded;
                }

                _windows.Add(sender.Identity, new RateWindow(now, 1));
                return IdentityRateLimitResult.Accepted;
            }

            if (window.Count >= LanProtocolPolicy.MaximumRequestsPerIdentity)
            {
                return IdentityRateLimitResult.RateLimited;
            }

            _windows[sender.Identity] = window with { Count = window.Count + 1 };
            return IdentityRateLimitResult.Accepted;
        }
    }

    private void RemoveExpiredWindows(DateTimeOffset now)
    {
        foreach (var entry in _windows
                     .Where(entry => entry.Value.StartedAt + LanProtocolPolicy.RateLimitWindow <= now)
                     .ToList())
        {
            _windows.Remove(entry.Key);
        }
    }

    private sealed record RateWindow(DateTimeOffset StartedAt, int Count);
}

public enum IdentityRateLimitResult
{
    Accepted,
    RateLimited,
    CapacityExceeded,
}
