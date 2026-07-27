using System.Security.Authentication;

namespace Unskip.Infrastructure.Windows.Messaging;

public sealed class MutualTlsAuthenticationException : AuthenticationException
{
    public MutualTlsAuthenticationException(string message)
        : base(message)
    {
    }

    public MutualTlsAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
