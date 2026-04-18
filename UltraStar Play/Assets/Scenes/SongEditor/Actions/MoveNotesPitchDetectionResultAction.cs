using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNotesToPitchDetectionResultAction : INeedInjection
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongEditorPitchDetectionControl songEditorPitchDetectionControl;

    [Inject]
    private GameObject gameObject;

    public void MoveNotesToDetectedPitch(List<Note> notes, bool notify)
    {
        PitchDetectionResult pitchDetectionResult = songEditorPitchDetectionControl.LastPitchDetectionResult;
        if (pitchDetectionResult == null)
        {
            return;
        }

        PitchDetectionNoteMover.MoveNotesToDetectedPitch(
            songMeta,
            notes,
            pitchDetectionResult);

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
    }
}
