/**
 * Indicates that an event has no value besides its presence.
 */
public class VoidEvent
{
    public static readonly VoidEvent instance = new VoidEvent();

    private VoidEvent()
    {
    }
}
