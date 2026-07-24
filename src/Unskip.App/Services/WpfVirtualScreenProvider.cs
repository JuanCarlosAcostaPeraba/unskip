using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfVirtualScreenProvider : IVirtualScreenProvider
{
    public VirtualScreenLayout GetLayout()
    {
        return new VirtualScreenLayout(
            new VirtualScreenBounds(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight),
            new VirtualScreenBounds(
                0,
                0,
                SystemParameters.PrimaryScreenWidth,
                SystemParameters.PrimaryScreenHeight));
    }
}
