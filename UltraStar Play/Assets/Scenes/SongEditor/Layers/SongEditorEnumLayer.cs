using System;
using System.Collections.Generic;
using UnityEngine;

public class SongEditorEnumLayer : AbstractSongEditorLayer
{
    public ESongEditorLayer LayerEnum { get; private set; }

    private readonly List<Note> notes = new();
    private readonly HashSet<Note> notesHashSet = new();

    public SongEditorEnumLayer(ESongEditorLayer layerEnum)
    {
        LayerEnum = layerEnum;
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

    public SongEditorEnumLayer CloneDeep()
    {
        SongEditorEnumLayer clone = new(LayerEnum);
        clone.Color = Color;
        clone.IsVisible = IsVisible;
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

    public override string GetDisplayName()
    {
        return LayerEnum.ToString();
    }
}
