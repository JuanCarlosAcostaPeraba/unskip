using Unskip.Core;

namespace Unskip.App.ViewModels;

/// <summary>
/// Supplies content for the initial application shell.
/// </summary>
public sealed class MainWindowViewModel
{
    public string ProductName { get; } = ProductIdentity.Name;

    public string Tagline { get; } = ProductIdentity.Tagline;

    public string AffiliationNotice { get; } = ProductIdentity.AffiliationNotice;

    public string CurrentSection { get; } = "Send";

    public string StatusMessage { get; } = "Message delivery will be added in a later issue.";

    public IReadOnlyList<string> NavigationItems { get; } = ["Send", "Devices", "History"];
}
