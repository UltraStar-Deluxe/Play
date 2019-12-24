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
        List<Note> notesCopy = new List<Note>(notes);
        return notesCopy;
    }

    public SongEditorLayer(ESongEditorLayer layerKey)
    {
        this.LayerKey = layerKey;
    }
}