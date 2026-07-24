using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfVirtualScreenProvider : IVirtualScreenProvider
{
    public VirtualScreenBounds GetBounds()
    {
        return new VirtualScreenBounds(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
    }
}
