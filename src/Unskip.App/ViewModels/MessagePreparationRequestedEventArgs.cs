using Unskip.Core.Devices;

namespace Unskip.App.ViewModels;

public sealed class MessagePreparationRequestedEventArgs(
    string alias,
    string destination,
    DeviceDestinationKind destinationKind,
    Guid? deviceId) : EventArgs
{
    public string Alias { get; } = alias;

    public string Destination { get; } = destination;

    public DeviceDestinationKind DestinationKind { get; } = destinationKind;

    public Guid? DeviceId { get; } = deviceId;
}
