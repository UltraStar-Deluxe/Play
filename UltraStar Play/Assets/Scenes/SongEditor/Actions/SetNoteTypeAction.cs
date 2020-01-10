using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SetNoteTypeAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public bool CanExecute(IReadOnlyCollection<Note> selectedNotes, ENoteType type)
    {
        return selectedNotes.AnyMatch(note => note.Type != type);
    }

    public void Execute(IReadOnlyCollection<Note> selectedNotes, ENoteType type)
    {
        foreach (Note note in selectedNotes)
        {
            note.SetType(type);
        }
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes, ENoteType type)
    {
        Execute(selectedNotes, type);
        songMetaChangeEventStream.OnNext(new NoteTypeChangeEvent());
    }
}
