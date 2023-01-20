using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractSongEditorLayer
{
    public bool IsVisible { get; set; } = true;
    public bool IsEditable { get; set; } = true;
    public Color Color { get; set; } = Colors.indigo;

    public abstract string GetDisplayName();
}
