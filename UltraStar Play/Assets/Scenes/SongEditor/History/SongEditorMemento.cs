using System.Collections.Generic;

public class SongEditorMemento
{
    public List<Voice> Voices { get; private set; } = new();
    public List<SongEditorLayer> Layers { get; private set; } = new();

    // Memento of SongMeta tags
    public float Bpm { get; set; }
    public float MusicGap { get; set; }
}
