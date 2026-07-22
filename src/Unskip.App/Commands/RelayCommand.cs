using System.Windows.Input;

namespace Unskip.App.Commands;

public sealed class RelayCommand(
    Action<object?> execute,
    Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Predicate<object?>? _canExecute = canExecute;
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
