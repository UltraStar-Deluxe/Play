using UnityEngine;

public class ScreenSizeChangedEvent
{
    public Vector2Int OldResolution { get; private set; }
    public Vector2Int NewResolution  { get; private set; }

    public ScreenSizeChangedEvent(Vector2Int oldResolution, Vector2Int newResolution)
    {
        OldResolution = oldResolution;
        NewResolution = newResolution;
    }

    public override string ToString()
    {
        return $"ScreenSizeChangedEvent(old: {OldResolution}, new: {NewResolution})";
    }
}
