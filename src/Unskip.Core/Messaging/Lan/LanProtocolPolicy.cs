namespace Unskip.Core.Messaging.Lan;

public static class LanProtocolPolicy
{
    public const int CurrentVersion = 1;

    public const int MaximumFramePayloadBytes = 16 * 1024;

    public const int NonceByteLength = 16;

    public const int ReplayCapacity = 4096;

    public const int MaximumTrackedIdentities = 1024;

    public const int MaximumRequestsPerIdentity = 10;

    public static readonly TimeSpan MaximumClockSkew = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan MaximumMessageLifetime = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
}
