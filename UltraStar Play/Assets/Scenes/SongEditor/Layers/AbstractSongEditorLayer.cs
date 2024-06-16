using UnityEngine;

public abstract class AbstractSongEditorLayer
{
    public bool IsVisible { get; set; } = true;
    public bool IsEditable { get; set; } = true;
    public bool IsMidiSoundPlayAlongEnabled { get; set; } = true;
    public Color Color { get; set; } = Colors.indigo;

    public abstract Translation GetDisplayName();
}
