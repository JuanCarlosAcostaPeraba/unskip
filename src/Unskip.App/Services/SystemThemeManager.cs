using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Unskip.App.Services;

internal static class SystemThemeManager
{
    private const string PersonalizeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static void Apply(ResourceDictionary resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        if (SystemParameters.HighContrast)
        {
            resources["AppBackgroundBrush"] = SystemColors.WindowBrush;
            resources["SurfaceBrush"] = SystemColors.WindowBrush;
            resources["SurfaceMutedBrush"] = SystemColors.ControlBrush;
            resources["TextPrimaryBrush"] = SystemColors.WindowTextBrush;
            resources["TextSecondaryBrush"] = SystemColors.GrayTextBrush;
            resources["BorderBrush"] = SystemColors.ActiveBorderBrush;
            resources["AccentBrush"] = SystemColors.HighlightBrush;
            resources["AccentDarkBrush"] = SystemColors.HotTrackBrush;
            resources["AccentSoftBrush"] = SystemColors.ControlBrush;
            return;
        }

        var preference = Registry.GetValue(PersonalizeKey, "AppsUseLightTheme", 1);
        if (preference is not int value || value != 0)
        {
            return;
        }

        resources["AppBackgroundBrush"] = CreateBrush("#101719");
        resources["SurfaceBrush"] = CreateBrush("#172124");
        resources["SurfaceMutedBrush"] = CreateBrush("#202D30");
        resources["SidebarBrush"] = CreateBrush("#091113");
        resources["SidebarMutedBrush"] = CreateBrush("#8FA6A8");
        resources["TextPrimaryBrush"] = CreateBrush("#F0F6F5");
        resources["TextSecondaryBrush"] = CreateBrush("#A8B8BA");
        resources["BorderBrush"] = CreateBrush("#304044");
        resources["AccentBrush"] = CreateBrush("#3BC2B7");
        resources["AccentDarkBrush"] = CreateBrush("#24A59C");
        resources["AccentSoftBrush"] = CreateBrush("#163B3A");
        resources["DangerBrush"] = CreateBrush("#FF8997");
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
