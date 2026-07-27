using System.Collections.ObjectModel;
using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.Core.Devices;
using Unskip.Core.Networking;
using Unskip.Core.Time;

namespace Unskip.App.ViewModels;

public sealed class DeviceDirectoryViewModel : ObservableObject
{
    private readonly List<DeviceListItemViewModel> _allDevices = [];
    private readonly IClock _clock;
    private readonly IDeviceDeletionConfirmation _deletionConfirmation;
    private readonly DeviceDirectoryService _directory;
    private bool _isBusy;
    private bool _isManualPrepared;
    private string? _manualDestination;
    private string? _manualDestinationError;
    private string? _preparedAlias;
    private string? _preparedDestination;
    private DeviceDestinationKind? _preparedDestinationKind;
    private Guid? _preparedDeviceId;
    private string? _preparedKindLabel;
    private string? _searchText;
    private DeviceListItemViewModel? _selectedDevice;
    private string _statusMessage = UiText.Get("DeviceDirectoryLocal");

    public DeviceDirectoryViewModel(
        DeviceDirectoryService directory,
        IClock clock,
        IDeviceDeletionConfirmation deletionConfirmation)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _deletionConfirmation = deletionConfirmation
            ?? throw new ArgumentNullException(nameof(deletionConfirmation));

        NewDeviceCommand = new RelayCommand(_ => BeginCreate(), _ => !IsBusy);
        EditDeviceCommand = new RelayCommand(_ => BeginEdit(), _ => SelectedDevice is not null && !IsBusy);
        CancelEditCommand = new RelayCommand(_ => CloseEditor(), _ => !IsBusy);
        SaveDeviceCommand = new AsyncRelayCommand(_ => SaveEditorAsync(), _ => Editor.IsOpen && !IsBusy);
        DeleteDeviceCommand = new AsyncRelayCommand(_ => DeleteSelectedAsync(), _ => SelectedDevice is not null && !IsBusy);
        ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavoriteAsync, parameter => parameter is DeviceListItemViewModel && !IsBusy);
        UseManualDestinationCommand = new RelayCommand(_ => PrepareManualDestination(), _ => !string.IsNullOrWhiteSpace(ManualDestination) && !IsBusy);
        SaveManualDestinationCommand = new RelayCommand(_ => BeginCreateFromManual(), _ => IsManualPrepared && !IsBusy);
        PrepareMessageCommand = new RelayCommand(_ => PrepareMessage(), _ => HasPreparedDestination && !IsBusy);
    }

    public event EventHandler<MessagePreparationRequestedEventArgs>? MessagePreparationRequested;

    public ObservableCollection<DeviceListItemViewModel> FilteredDevices { get; } = [];

    public DeviceEditorViewModel Editor { get; } = new();

    public RelayCommand NewDeviceCommand { get; }

    public RelayCommand EditDeviceCommand { get; }

    public RelayCommand CancelEditCommand { get; }

    public AsyncRelayCommand SaveDeviceCommand { get; }

    public AsyncRelayCommand DeleteDeviceCommand { get; }

    public AsyncRelayCommand ToggleFavoriteCommand { get; }

    public RelayCommand UseManualDestinationCommand { get; }

    public RelayCommand SaveManualDestinationCommand { get; }

    public RelayCommand PrepareMessageCommand { get; }

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
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
                ManualDestinationError = null;
                UseManualDestinationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ManualDestinationError
    {
        get => _manualDestinationError;
        private set => SetProperty(ref _manualDestinationError, value);
    }

    public DeviceListItemViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                OnPropertyChanged(nameof(HasSelectedDevice));
                NotifyCommandStates();
                if (value is not null)
                {
                    PrepareSavedDestination(value);
                }
                else if (!IsManualPrepared)
                {
                    ClearPreparedDestination();
                }
            }
        }
    }

    public bool HasSelectedDevice => SelectedDevice is not null;

    public string? PreparedAlias
    {
        get => _preparedAlias;
        private set => SetProperty(ref _preparedAlias, value);
    }

    public string? PreparedDestination
    {
        get => _preparedDestination;
        private set
        {
            if (SetProperty(ref _preparedDestination, value))
            {
                OnPropertyChanged(nameof(HasPreparedDestination));
                PrepareMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? PreparedKindLabel
    {
        get => _preparedKindLabel;
        private set => SetProperty(ref _preparedKindLabel, value);
    }

    public DeviceDestinationKind? PreparedDestinationKind
    {
        get => _preparedDestinationKind;
        private set => SetProperty(ref _preparedDestinationKind, value);
    }

    public Guid? PreparedDeviceId
    {
        get => _preparedDeviceId;
        private set => SetProperty(ref _preparedDeviceId, value);
    }

    public bool HasPreparedDestination => !string.IsNullOrWhiteSpace(PreparedDestination);

    public bool IsManualPrepared
    {
        get => _isManualPrepared;
        private set
        {
            if (SetProperty(ref _isManualPrepared, value))
            {
                SaveManualDestinationCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string DeviceCountLabel => _allDevices.Count switch
    {
        0 => UiText.Get("NoSavedDevices"),
        1 => UiText.Get("OneSavedDevice"),
        var count => UiText.Format("SavedDeviceCount", count),
    };

    public string FilterResultLabel => string.IsNullOrWhiteSpace(SearchText)
        ? DeviceCountLabel
        : UiText.Format("MatchingDeviceCount", FilteredDevices.Count);

    public async Task InitializeAsync()
    {
        await ReloadAsync().ConfigureAwait(true);
    }

    private void BeginCreate()
    {
        Editor.BeginCreate();
        NotifyCommandStates();
    }

    private void BeginCreateFromManual()
    {
        if (!IsManualPrepared || string.IsNullOrWhiteSpace(PreparedDestination))
        {
            return;
        }

        if (PreparedDestinationKind is not DeviceDestinationKind kind)
        {
            return;
        }

        Editor.BeginCreateFromManual(kind, PreparedDestination);
        NotifyCommandStates();
    }

    private void BeginEdit()
    {
        if (SelectedDevice is null)
        {
            return;
        }

        Editor.BeginEdit(SelectedDevice.Device);
        NotifyCommandStates();
    }

    private void CloseEditor()
    {
        Editor.Close();
        NotifyCommandStates();
    }

    private async Task SaveEditorAsync()
    {
        IsBusy = true;
        try
        {
            var result = Editor.DeviceId is Guid id
                ? await _directory.UpdateAsync(id, Editor.CreateInput()).ConfigureAwait(true)
                : await _directory.CreateAsync(Editor.CreateInput()).ConfigureAwait(true);

            switch (result.Status)
            {
                case DeviceMutationStatus.Succeeded:
                    Editor.Close();
                    await ReloadAsync(result.Device!.Id).ConfigureAwait(true);
                    StatusMessage = UiText.Format("DeviceSaved", result.Device.Alias);
                    break;
                case DeviceMutationStatus.ValidationFailed:
                    Editor.ApplyErrors(result.ValidationErrors);
                    StatusMessage = UiText.Get("CheckHighlightedFields");
                    break;
                case DeviceMutationStatus.Conflict:
                    Editor.ShowConflict();
                    StatusMessage = UiText.Get("DeviceConflict");
                    break;
                case DeviceMutationStatus.NotFound:
                    Editor.Close();
                    await ReloadAsync().ConfigureAwait(true);
                    StatusMessage = UiText.Get("DeviceMissing");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result.Status, null);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedDevice is null
            || !await _deletionConfirmation.ConfirmAsync(SelectedDevice.Alias).ConfigureAwait(true))
        {
            return;
        }

        var alias = SelectedDevice.Alias;
        IsBusy = true;
        try
        {
            var result = await _directory.DeleteAsync(SelectedDevice.Id).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
            StatusMessage = result.IsSuccessful
                ? UiText.Format("DeviceDeleted", alias)
                : UiText.Get("DeviceMissing");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleFavoriteAsync(object? parameter)
    {
        if (parameter is not DeviceListItemViewModel item)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _directory.SetFavoriteAsync(item.Id, !item.IsFavorite).ConfigureAwait(true);
            await ReloadAsync(item.Id).ConfigureAwait(true);
            StatusMessage = result.IsSuccessful
                ? UiText.Format(
                    item.IsFavorite ? "DeviceRemovedFromFavorites" : "DeviceAddedToFavorites",
                    item.Alias)
                : UiText.Get("DeviceMissing");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PrepareSavedDestination(DeviceListItemViewModel item)
    {
        PreparedAlias = item.Alias;
        PreparedDestination = item.ResolvedDestination;
        PreparedKindLabel = item.PreferredDestinationLabel;
        PreparedDestinationKind = item.Device.PreferredDestination;
        PreparedDeviceId = item.Id;
        IsManualPrepared = false;
        StatusMessage = UiText.Format("DeviceSelected", item.Alias, item.ResolvedDestination);
    }

    private void PrepareManualDestination()
    {
        var candidate = ManualDestination?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            ManualDestinationError = UiText.Get("EnterDestination");
            return;
        }

        string? normalized;
        string kindLabel;
        DeviceDestinationKind destinationKind;
        if (NetworkAddressValidator.TryNormalizeCanonicalIpv4(candidate, out normalized))
        {
            kindLabel = UiText.Get("Ipv4Address");
            destinationKind = DeviceDestinationKind.Ipv4;
        }
        else if (candidate.All(character => char.IsDigit(character) || character == '.'))
        {
            ManualDestinationError = UiText.Get("EnterCanonicalIpv4");
            return;
        }
        else if (NetworkAddressValidator.TryNormalizeHostname(candidate, out normalized))
        {
            kindLabel = UiText.Get("ComputerName");
            destinationKind = DeviceDestinationKind.Hostname;
        }
        else
        {
            ManualDestinationError = UiText.Get("UseValidDestination");
            return;
        }

        SelectedDevice = null;
        PreparedAlias = UiText.Get("ManualDestination");
        PreparedDestination = normalized;
        PreparedKindLabel = kindLabel;
        PreparedDestinationKind = destinationKind;
        PreparedDeviceId = null;
        IsManualPrepared = true;
        ManualDestinationError = null;
        StatusMessage = UiText.Format("ManualDestinationReady", normalized);
    }

    private void PrepareMessage()
    {
        if (PreparedAlias is null
            || PreparedDestination is null
            || PreparedDestinationKind is not DeviceDestinationKind destinationKind)
        {
            return;
        }

        StatusMessage = UiText.Format("OpeningMessage", PreparedAlias);
        MessagePreparationRequested?.Invoke(
            this,
            new MessagePreparationRequestedEventArgs(
                PreparedAlias,
                PreparedDestination,
                destinationKind,
                PreparedDeviceId,
                SelectedDevice?.ComputerName ?? (destinationKind == DeviceDestinationKind.Hostname ? PreparedDestination : null),
                SelectedDevice?.Ipv4Address ?? (destinationKind == DeviceDestinationKind.Ipv4 ? PreparedDestination : null)));
    }

    private void ClearPreparedDestination()
    {
        PreparedAlias = null;
        PreparedDestination = null;
        PreparedKindLabel = null;
        PreparedDestinationKind = null;
        PreparedDeviceId = null;
    }

    private async Task ReloadAsync(Guid? selectedId = null)
    {
        var devices = await _directory.GetAllAsync().ConfigureAwait(true);
        _allDevices.Clear();
        _allDevices.AddRange(devices.Select(device => new DeviceListItemViewModel(device, _clock.UtcNow)));
        ApplyFilter();

        SelectedDevice = selectedId.HasValue
            ? FilteredDevices.FirstOrDefault(device => device.Id == selectedId.Value)
            : null;
        OnPropertyChanged(nameof(DeviceCountLabel));
        OnPropertyChanged(nameof(FilterResultLabel));
    }

    private void ApplyFilter()
    {
        var search = SearchText?.Trim();
        var matchingDevices = string.IsNullOrWhiteSpace(search)
            ? _allDevices
            : _allDevices.Where(device => Matches(device, search)).ToList();

        FilteredDevices.Clear();
        foreach (var device in matchingDevices)
        {
            FilteredDevices.Add(device);
        }

        if (SelectedDevice is not null
            && FilteredDevices.All(device => device.Id != SelectedDevice.Id))
        {
            SelectedDevice = null;
        }

        OnPropertyChanged(nameof(FilterResultLabel));
    }

    private static bool Matches(DeviceListItemViewModel item, string search)
    {
        return Contains(item.Device.Alias, search)
            || Contains(item.Device.ComputerName, search)
            || Contains(item.Device.Ipv4Address, search)
            || Contains(item.Device.Description, search);
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void NotifyCommandStates()
    {
        NewDeviceCommand.NotifyCanExecuteChanged();
        EditDeviceCommand.NotifyCanExecuteChanged();
        CancelEditCommand.NotifyCanExecuteChanged();
        SaveDeviceCommand.NotifyCanExecuteChanged();
        DeleteDeviceCommand.NotifyCanExecuteChanged();
        ToggleFavoriteCommand.NotifyCanExecuteChanged();
        UseManualDestinationCommand.NotifyCanExecuteChanged();
        SaveManualDestinationCommand.NotifyCanExecuteChanged();
        PrepareMessageCommand.NotifyCanExecuteChanged();
    }
}
