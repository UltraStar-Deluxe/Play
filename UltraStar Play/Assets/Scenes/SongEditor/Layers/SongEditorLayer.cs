using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SongEditorLayer
{
    public ESongEditorLayer LayerKey { get; private set; }
    public bool IsEnabled { get; set; } = true;
    public Color Color { get; set; } = Colors.indigo;

    private readonly List<Note> notes = new List<Note>();

    public SongEditorLayer(ESongEditorLayer layerKey)
    {
        this.LayerKey = layerKey;
    }

    public void AddNote(Note note)
    {
        notes.Add(note);
    }

    public void RemoveNote(Note note)
    {
        notes.Remove(note);
    }

    public List<Note> GetNotes()
    {
        return new List<Note>(notes);
    }

    public SongEditorLayer CloneDeep()
    {
        SongEditorLayer clone = new SongEditorLayer(LayerKey);
        clone.Color = Color;
        clone.IsEnabled = IsEnabled;
        foreach (Note note in notes)
        {
            Note noteCopy = note.Clone();
            clone.AddNote(noteCopy);
        }
        return clone;
    }

    public void ClearNotes()
    {
        notes.Clear();
    }
}