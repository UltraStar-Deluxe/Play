using System.Collections.Generic;
using System.Linq;

public class ConnectedMicDevicesChangedEvent
{
    public List<string> CurrentConnectedMicDevices { get; private set; }
    public List<string> OldConnectedMicDevices { get; private set; }

    public ConnectedMicDevicesChangedEvent(IEnumerable<string> currentConnectedMicDevices, IEnumerable<string> oldConnectedMicDevices)
    {
        this.CurrentConnectedMicDevices = currentConnectedMicDevices.ToList();
        this.OldConnectedMicDevices = oldConnectedMicDevices.ToList();
    }
}
