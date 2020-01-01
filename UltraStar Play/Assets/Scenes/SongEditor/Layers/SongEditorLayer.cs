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

    public void ClearNotes()
    {
        notes.Clear();
    }
}