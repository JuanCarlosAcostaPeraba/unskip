using System.Net;
using System.Net.Sockets;

namespace Unskip.Infrastructure.Windows;

internal sealed class SystemDnsLookup : IDnsLookup
{
    public Task<IPHostEntry> GetHostEntryAsync(
        IPAddress address,
        CancellationToken cancellationToken)
    {
        return Dns.GetHostEntryAsync(address).WaitAsync(cancellationToken);
    }

    public Task<IPAddress[]> GetHostAddressesAsync(
        string hostName,
        CancellationToken cancellationToken)
    {
        return Dns.GetHostAddressesAsync(
            hostName,
            AddressFamily.Unspecified,
            cancellationToken);
    }
}
