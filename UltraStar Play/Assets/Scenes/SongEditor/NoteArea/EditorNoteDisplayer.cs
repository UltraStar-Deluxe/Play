using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

#pragma warning disable CS0649

public class EditorNoteDisplayer : MonoBehaviour, INeedInjection
{

    [InjectedInInspector]
    public UiEditorNote notePrefab;

    [InjectedInInspector]
    public RectTransform noteContainer;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    void Start()
    {
        noteArea.ViewportEventStream.Subscribe(_ => UpdateEditorNotes());
    }

    private void UpdateEditorNotes()
    {
        noteContainer.DestroyAllDirectChildren();

        int minBeat = (int)Math.Floor(noteArea.GetMinBeatInViewport());
        int maxBeat = (int)Math.Ceiling(noteArea.GetMaxBeatInViewport());

        List<Sentence> sentencesInViewport = voices
            .SelectMany(voice => voice.Sentences)
            .Where(sentence => sentence.StartBeat <= maxBeat && sentence.EndBeat >= minBeat)
            .ToList();

        List<Note> notesInViewport = sentencesInViewport
            .SelectMany(sentence => sentence.Notes)
            .Where(sentence => sentence.StartBeat <= maxBeat && sentence.EndBeat >= minBeat)
            .ToList();

        foreach (Note note in notesInViewport)
        {
            CreateEditorNote(note);
        }
    }

    private void CreateEditorNote(Note note)
    {
        if (note.StartBeat == note.EndBeat)
        {
            return;
        }

        UiEditorNote uiNote = Instantiate(notePrefab, noteContainer);
        injector.Inject(uiNote);
        uiNote.Init(note);

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);
    }

    private void PositionUiNote(RectTransform uiNoteRectTransform, int midiNote, int startBeat, int endBeat)
    {
        float y = noteArea.GetVerticalPositionForGeneralMidiNote(midiNote);
        float xStart = noteArea.GetHorizontalPositionForBeat(startBeat);
        float xEnd = noteArea.GetHorizontalPositionForBeat(endBeat);
        float height = noteArea.GetHeightForSingleNote();

        uiNoteRectTransform.anchorMin = new Vector2(xStart, y - height / 2f);
        uiNoteRectTransform.anchorMax = new Vector2(xEnd, y + height / 2f);
        uiNoteRectTransform.anchoredPosition = Vector2.zero;
        uiNoteRectTransform.sizeDelta = Vector2.zero;
    }
}
