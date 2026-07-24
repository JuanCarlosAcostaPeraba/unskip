using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows;

internal interface IWindowsMsgServerResolver
{
    Task<WindowsMsgServerResolution> ResolveAsync(
        MessageTarget target,
        CancellationToken cancellationToken);
}
