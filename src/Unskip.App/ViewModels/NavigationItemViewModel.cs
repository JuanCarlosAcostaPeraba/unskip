using System.Windows.Input;

namespace Unskip.App.ViewModels;

public sealed record NavigationItemViewModel(
    string Label,
    string Symbol,
    bool IsActive,
    string? Badge = null,
    ICommand? Command = null);
