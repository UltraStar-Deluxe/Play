using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SongEditorLayer
{
    public ESongEditorLayer LayerEnum { get; private set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsEditable { get; set; } = true;
    public Color Color { get; set; } = Colors.indigo;

    private readonly List<Note> notes = new();
    private readonly HashSet<Note> notesHashSet = new();

    public SongEditorLayer(ESongEditorLayer layerEnum)
    {
        this.LayerEnum = layerEnum;
    }

    public void AddNote(Note note)
    {
        notes.Add(note);
        notesHashSet.Add(note);
    }

    public void RemoveNote(Note note)
    {
        notes.Remove(note);
        notesHashSet.Remove(note);
    }

    public bool ContainsNote(Note note)
    {
        return notesHashSet.Contains(note);
    }

    public List<Note> GetNotes()
    {
        return new List<Note>(notes);
    }

    public SongEditorLayer CloneDeep()
    {
        SongEditorLayer clone = new(LayerEnum);
        clone.Color = Color;
        clone.IsEnabled = IsEnabled;
        notes.ForEach(note =>
        {
            Note noteCopy = note.Clone();
            clone.AddNote(noteCopy);
        });
        return clone;
    }

    public void ClearNotes()
    {
        notes.Clear();
    }
}
