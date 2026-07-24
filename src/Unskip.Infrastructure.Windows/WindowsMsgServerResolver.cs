using System.Net;
using System.Net.Sockets;
using Unskip.Core.Messaging;
using Unskip.Core.Networking;

namespace Unskip.Infrastructure.Windows;

internal sealed class WindowsMsgServerResolver : IWindowsMsgServerResolver
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumTimeout = TimeSpan.FromSeconds(30);

    private readonly IDnsLookup _dnsLookup;
    private readonly TimeSpan _timeout;

    public WindowsMsgServerResolver()
        : this(new SystemDnsLookup(), DefaultTimeout)
    {
    }

    internal WindowsMsgServerResolver(IDnsLookup dnsLookup, TimeSpan? timeout = null)
    {
        _dnsLookup = dnsLookup ?? throw new ArgumentNullException(nameof(dnsLookup));
        _timeout = timeout ?? DefaultTimeout;

        if (_timeout <= TimeSpan.Zero || _timeout > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                _timeout.TotalSeconds,
                $"DNS timeout must be greater than zero and no more than {MaximumTimeout.TotalSeconds} seconds.");
        }
    }

    public async Task<WindowsMsgServerResolution> ResolveAsync(
        MessageTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        cancellationToken.ThrowIfCancellationRequested();

        if (target.Kind == MessageTargetKind.Hostname)
        {
            return WindowsMsgServerResolution.Success(target.Value);
        }

        if (target.Kind != MessageTargetKind.Ipv4Address
            || !IPAddress.TryParse(target.Value, out var requestedAddress)
            || requestedAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            return WindowsMsgServerResolution.Failure(
                "The destination was not a validated hostname or canonical IPv4 address.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);

        try
        {
            var reverseEntry = await _dnsLookup.GetHostEntryAsync(
                requestedAddress,
                timeoutSource.Token).ConfigureAwait(false);

            if (NetworkAddressValidator.IsIpv4Address(reverseEntry.HostName)
                || !NetworkAddressValidator.TryNormalizeHostname(reverseEntry.HostName, out var hostName))
            {
                return WindowsMsgServerResolution.Failure(
                    "Reverse DNS did not return a valid computer name.");
            }

            var forwardAddresses = await _dnsLookup.GetHostAddressesAsync(
                hostName!,
                timeoutSource.Token).ConfigureAwait(false);
            var matchesRequestedAddress = forwardAddresses.Any(
                address => address.AddressFamily == AddressFamily.InterNetwork
                    && address.Equals(requestedAddress));

            return matchesRequestedAddress
                ? WindowsMsgServerResolution.Success(hostName!)
                : WindowsMsgServerResolution.Failure(
                    "The reverse DNS name did not resolve back to the requested IPv4 address.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return WindowsMsgServerResolution.Failure(
                "DNS verification did not finish before the timeout.");
        }
        catch (SocketException)
        {
            return WindowsMsgServerResolution.Failure(
                "DNS could not resolve a computer name for the IPv4 destination.");
        }
        catch (ArgumentException)
        {
            return WindowsMsgServerResolution.Failure(
                "DNS returned an invalid computer name for the IPv4 destination.");
        }
    }
}
