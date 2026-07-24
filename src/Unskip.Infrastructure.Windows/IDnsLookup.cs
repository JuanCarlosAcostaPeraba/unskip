using System.Net;

namespace Unskip.Infrastructure.Windows;

internal interface IDnsLookup
{
    Task<IPHostEntry> GetHostEntryAsync(
        IPAddress address,
        CancellationToken cancellationToken);

    Task<IPAddress[]> GetHostAddressesAsync(
        string hostName,
        CancellationToken cancellationToken);
}
