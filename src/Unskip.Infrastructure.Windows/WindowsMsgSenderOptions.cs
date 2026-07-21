namespace Unskip.Infrastructure.Windows;

public sealed class WindowsMsgSenderOptions
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromMinutes(2);

    public WindowsMsgSenderOptions(TimeSpan? timeout = null)
    {
        Timeout = timeout ?? DefaultTimeout;
        if (Timeout <= TimeSpan.Zero || Timeout > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                $"The timeout must be greater than zero and no more than {MaximumTimeout.TotalSeconds} seconds.");
        }
    }

    public TimeSpan Timeout { get; }
}
