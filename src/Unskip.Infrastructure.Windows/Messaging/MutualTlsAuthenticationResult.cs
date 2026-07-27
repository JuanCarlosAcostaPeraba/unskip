using System.Net.Security;
using Unskip.Core.Messaging.Lan;

namespace Unskip.Infrastructure.Windows.Messaging;

public sealed class MutualTlsAuthenticationResult : IAsyncDisposable
{
    internal MutualTlsAuthenticationResult(
        SslStream protectedStream,
        AuthenticatedSessionContext session)
    {
        ProtectedStream = protectedStream;
        Session = session;
    }

    public SslStream ProtectedStream { get; }

    public AuthenticatedSessionContext Session { get; }

    public ValueTask DisposeAsync()
    {
        return ProtectedStream.DisposeAsync();
    }
}
