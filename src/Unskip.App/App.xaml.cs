using System.Windows;
using Unskip.Infrastructure.Persistence;

namespace Unskip.App;

/// <summary>
/// Represents the WPF application entry point.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var database = UnskipDatabase.ForCurrentUser();
        database.InitializeAsync().GetAwaiter().GetResult();

        base.OnStartup(e);
    }
}
