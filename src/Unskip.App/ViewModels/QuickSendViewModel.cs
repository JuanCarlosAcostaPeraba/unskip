using System.Collections.ObjectModel;
using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.Core.Devices;
using Unskip.Core.Networking;
using Unskip.Core.Time;

namespace Unskip.App.ViewModels;

public sealed class QuickSendViewModel : ObservableObject
{
    private readonly IClock _clock;
    private readonly DeviceDirectoryService _directory;
    private string? _destinationError;
    private string? _manualDestination;
    private DeviceListItemViewModel? _selectedDevice;
    private string _statusMessage = UiText.Get("QuickChooseDestination");

    public QuickSendViewModel(
        DeviceDirectoryService directory,
        IClock clock,
        MessageComposerViewModel composer)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Composer = composer ?? throw new ArgumentNullException(nameof(composer));
        UseManualDestinationCommand = new RelayCommand(
            _ => UseManualDestination(),
            _ => !string.IsNullOrWhiteSpace(ManualDestination) && !Composer.IsSending);
        OpenMainWindowCommand = new RelayCommand(
            _ => OpenMainWindowRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? OpenMainWindowRequested;

    public ObservableCollection<DeviceListItemViewModel> Devices { get; } = [];

    public MessageComposerViewModel Composer { get; }

    public RelayCommand UseManualDestinationCommand { get; }

    public RelayCommand OpenMainWindowCommand { get; }

    public DeviceListItemViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value) && value is not null)
            {
                _manualDestination = null;
                OnPropertyChanged(nameof(ManualDestination));
                DestinationError = null;
                PrepareDevice(value);
            }
        }
    }

    public string? ManualDestination
    {
        get => _manualDestination;
        set
        {
            if (SetProperty(ref _manualDestination, value))
            {
                DestinationError = null;
                UseManualDestinationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? DestinationError
    {
        get => _destinationError;
        private set => SetProperty(ref _destinationError, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task ReloadAsync()
    {
        var selectedId = SelectedDevice?.Id ?? Composer.DeviceId;
        try
        {
            var devices = await _directory.GetAllAsync().ConfigureAwait(true);
            var items = devices
                .Select(device => new DeviceListItemViewModel(device, _clock.UtcNow))
                .OrderByDescending(device => device.IsFavorite)
                .ThenByDescending(device => device.Device.LastUsedAt)
                .ThenBy(device => device.Alias, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Devices.Clear();
            foreach (var item in items)
            {
                Devices.Add(item);
            }

            if (selectedId is Guid id)
            {
                SelectedDevice = Devices.FirstOrDefault(device => device.Id == id);
            }

            StatusMessage = Devices.Count == 0
                ? UiText.Get("QuickNoSavedDevices")
                : UiText.Get("QuickChooseDestination");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = UiText.Get("QuickDeviceLoadFailed");
        }
    }

    private void PrepareDevice(DeviceListItemViewModel item)
    {
        var device = item.Device;
        Composer.Prepare(new MessagePreparationRequestedEventArgs(
            device.Alias,
            item.ResolvedDestination,
            device.PreferredDestination,
            device.Id,
            device.ComputerName,
            device.Ipv4Address));
        StatusMessage = UiText.Format("QuickDestinationReady", device.Alias);
    }

    private void UseManualDestination()
    {
        var candidate = ManualDestination?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            DestinationError = UiText.Get("EnterDestination");
            Composer.ClearPreparation();
            return;
        }

        string? normalized;
        DeviceDestinationKind destinationKind;
        if (NetworkAddressValidator.TryNormalizeCanonicalIpv4(candidate, out normalized))
        {
            destinationKind = DeviceDestinationKind.Ipv4;
        }
        else if (candidate.All(character => char.IsDigit(character) || character == '.'))
        {
            DestinationError = UiText.Get("EnterCanonicalIpv4");
            Composer.ClearPreparation();
            return;
        }
        else if (NetworkAddressValidator.TryNormalizeHostname(candidate, out normalized))
        {
            destinationKind = DeviceDestinationKind.Hostname;
        }
        else
        {
            DestinationError = UiText.Get("UseValidDestination");
            Composer.ClearPreparation();
            return;
        }

        SetProperty(ref _selectedDevice, null, nameof(SelectedDevice));
        Composer.Prepare(new MessagePreparationRequestedEventArgs(
            UiText.Get("ManualDestination"),
            normalized!,
            destinationKind,
            null,
            destinationKind == DeviceDestinationKind.Hostname ? normalized : null,
            destinationKind == DeviceDestinationKind.Ipv4 ? normalized : null));
        DestinationError = null;
        StatusMessage = UiText.Format("QuickDestinationReady", normalized);
    }
}
