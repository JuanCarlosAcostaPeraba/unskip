using Unskip.App.Localization;
using Unskip.Core.Devices;

namespace Unskip.App.ViewModels;

public sealed class DeviceEditorViewModel : ObservableObject
{
    private string? _alias;
    private string? _aliasError;
    private string? _computerName;
    private string? _computerNameError;
    private string? _description;
    private string? _descriptionError;
    private string? _destinationError;
    private string? _generalError;
    private string? _ipv4Address;
    private string? _ipv4AddressError;
    private bool _isFavorite;
    private bool _isOpen;
    private DeviceDestinationKind _preferredDestination = DeviceDestinationKind.Hostname;
    private string? _preferredDestinationError;

    public Guid? DeviceId { get; private set; }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public string Title => DeviceId.HasValue
        ? UiText.Get("EditDevice")
        : UiText.Get("AddDevicePlain");

    public string? Alias
    {
        get => _alias;
        set
        {
            if (SetProperty(ref _alias, value))
            {
                AliasError = null;
            }
        }
    }

    public string? ComputerName
    {
        get => _computerName;
        set
        {
            if (SetProperty(ref _computerName, value))
            {
                ComputerNameError = null;
                DestinationError = null;
            }
        }
    }

    public string? Ipv4Address
    {
        get => _ipv4Address;
        set
        {
            if (SetProperty(ref _ipv4Address, value))
            {
                Ipv4AddressError = null;
                DestinationError = null;
            }
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                DescriptionError = null;
            }
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public bool PreferHostname
    {
        get => PreferredDestination == DeviceDestinationKind.Hostname;
        set
        {
            if (value)
            {
                PreferredDestination = DeviceDestinationKind.Hostname;
            }
        }
    }

    public bool PreferIpv4
    {
        get => PreferredDestination == DeviceDestinationKind.Ipv4;
        set
        {
            if (value)
            {
                PreferredDestination = DeviceDestinationKind.Ipv4;
            }
        }
    }

    public string? AliasError
    {
        get => _aliasError;
        private set => SetProperty(ref _aliasError, value);
    }

    public string? ComputerNameError
    {
        get => _computerNameError;
        private set => SetProperty(ref _computerNameError, value);
    }

    public string? Ipv4AddressError
    {
        get => _ipv4AddressError;
        private set => SetProperty(ref _ipv4AddressError, value);
    }

    public string? DestinationError
    {
        get => _destinationError;
        private set => SetProperty(ref _destinationError, value);
    }

    public string? DescriptionError
    {
        get => _descriptionError;
        private set => SetProperty(ref _descriptionError, value);
    }

    public string? PreferredDestinationError
    {
        get => _preferredDestinationError;
        private set => SetProperty(ref _preferredDestinationError, value);
    }

    public string? GeneralError
    {
        get => _generalError;
        private set => SetProperty(ref _generalError, value);
    }

    private DeviceDestinationKind PreferredDestination
    {
        get => _preferredDestination;
        set
        {
            if (SetProperty(ref _preferredDestination, value))
            {
                OnPropertyChanged(nameof(PreferHostname));
                OnPropertyChanged(nameof(PreferIpv4));
                PreferredDestinationError = null;
            }
        }
    }

    public DeviceInput CreateInput()
    {
        return new DeviceInput(
            Alias,
            ComputerName,
            Ipv4Address,
            Description,
            IsFavorite,
            PreferredDestination);
    }

    public void BeginCreate()
    {
        DeviceId = null;
        Alias = null;
        ComputerName = null;
        Ipv4Address = null;
        Description = null;
        IsFavorite = false;
        PreferredDestination = DeviceDestinationKind.Hostname;
        ClearErrors();
        OnPropertyChanged(nameof(Title));
        IsOpen = true;
    }

    public void BeginCreateFromManual(
        DeviceDestinationKind destinationKind,
        string destination)
    {
        BeginCreate();
        PreferredDestination = destinationKind;
        if (destinationKind == DeviceDestinationKind.Hostname)
        {
            ComputerName = destination;
        }
        else
        {
            Ipv4Address = destination;
        }
    }

    public void BeginEdit(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        DeviceId = device.Id;
        Alias = device.Alias;
        ComputerName = device.ComputerName;
        Ipv4Address = device.Ipv4Address;
        Description = device.Description;
        IsFavorite = device.IsFavorite;
        PreferredDestination = device.PreferredDestination;
        ClearErrors();
        OnPropertyChanged(nameof(Title));
        IsOpen = true;
    }

    public void Close()
    {
        ClearErrors();
        IsOpen = false;
    }

    public void ApplyErrors(IReadOnlyList<DeviceValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ClearErrors();

        foreach (var error in errors)
        {
            var localizedMessage = LocalizeError(error);
            switch (error.Field)
            {
                case "Alias":
                    AliasError ??= localizedMessage;
                    break;
                case "ComputerName":
                    ComputerNameError ??= localizedMessage;
                    break;
                case "Ipv4Address":
                    Ipv4AddressError ??= localizedMessage;
                    break;
                case "Destination":
                    DestinationError ??= localizedMessage;
                    break;
                case "Description":
                    DescriptionError ??= localizedMessage;
                    break;
                case "PreferredDestination":
                    PreferredDestinationError ??= localizedMessage;
                    break;
                default:
                    GeneralError ??= localizedMessage;
                    break;
            }
        }
    }

    public void ShowConflict()
    {
        GeneralError = UiText.Get("DeviceAlreadySaved");
    }

    private static string LocalizeError(DeviceValidationError error) =>
        error.Code switch
        {
            DeviceValidationErrorCode.Required => UiText.Get("ValidationAliasRequired"),
            DeviceValidationErrorCode.TooLong when error.Field == "Alias" =>
                UiText.Format("ValidationAliasTooLong", DevicePolicy.MaximumAliasLength),
            DeviceValidationErrorCode.TooLong =>
                UiText.Format("ValidationDescriptionTooLong", DevicePolicy.MaximumDescriptionLength),
            DeviceValidationErrorCode.InvalidCharacters when error.Field == "Alias" =>
                UiText.Get("ValidationAliasCharacters"),
            DeviceValidationErrorCode.InvalidCharacters =>
                UiText.Get("ValidationDescriptionCharacters"),
            DeviceValidationErrorCode.InvalidHostname => UiText.Get("ValidationHostname"),
            DeviceValidationErrorCode.InvalidIpv4 => UiText.Get("EnterCanonicalIpv4"),
            DeviceValidationErrorCode.DestinationRequired => UiText.Get("ValidationDestinationRequired"),
            DeviceValidationErrorCode.PreferredDestinationUnavailable =>
                UiText.Get("ValidationPreferredDestination"),
            _ => error.Message,
        };

    private void ClearErrors()
    {
        AliasError = null;
        ComputerNameError = null;
        Ipv4AddressError = null;
        DestinationError = null;
        DescriptionError = null;
        PreferredDestinationError = null;
        GeneralError = null;
    }
}
