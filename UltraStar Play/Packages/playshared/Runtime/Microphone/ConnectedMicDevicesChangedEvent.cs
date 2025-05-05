using System.Collections.Generic;
using System.Linq;

public class ConnectedMicDevicesChangedEvent
{
    public IReadOnlyList<string> CurrentConnectedMicDevices { get; private set; }
    public IReadOnlyList<string> OldConnectedMicDevices { get; private set; }

    public IReadOnlyList<string> ConnectedMicDevices { get; private set; }
    public IReadOnlyList<string> DisconnectedMicDevices { get; private set; }
    
    public ConnectedMicDevicesChangedEvent(IEnumerable<string> currentConnectedMicDevices, IEnumerable<string> oldConnectedMicDevices)
    {
        this.CurrentConnectedMicDevices = currentConnectedMicDevices.ToList();
        this.OldConnectedMicDevices = oldConnectedMicDevices.ToList();

        ConnectedMicDevices = CurrentConnectedMicDevices
            .Except(OldConnectedMicDevices)
            .ToList();
        DisconnectedMicDevices = OldConnectedMicDevices
            .Except(CurrentConnectedMicDevices)
            .ToList();
    }
}
