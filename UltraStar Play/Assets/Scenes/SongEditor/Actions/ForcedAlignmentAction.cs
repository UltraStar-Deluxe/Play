using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ForcedAlignmentAction : AbstractAudioClipAction
{
    [Inject] private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject] private SongAudioPlayer songAudioPlayer;

    [Inject] private SongEditorLayerManager songEditorLayerManager;

    [Inject] private EditorNoteDisplayer editorNoteDisplayer;

    [Inject] private JobManager jobManager;

    [Inject] private SongMeta songMeta;

    [Inject] private ForcedAlignmentManager forcedAlignmentManager;

    public async Awaitable<ForcedAlignmentResult> RunForcedAlignment(string lyrics, bool notify)
    {
        ForcedAlignmentResult forcedAlignmentResult = await forcedAlignmentManager.ProcessSongMetaJob(
                songMeta,
                lyrics)
            .GetResultAsync();

        List<Note> notes = forcedAlignmentResult.Words
            .Select(wordTimestamp =>
            {
                double startInMillis = wordTimestamp.StartTime * 1000;
                double endInMillis = wordTimestamp.EndTime * 1000;
                int startBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, startInMillis);
                int endBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, endInMillis);
                int lengthInBeats = Math.Max(1, endBeat - startBeat);
                return new Note(
                    ENoteType.Normal,
                    startBeat,
                    lengthInBeats,
                    MidiUtils.GetUltraStarTxtPitch(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
                    wordTimestamp.Word);
            })
            .ToList();

        // Remove old notes
        ESongEditorLayer layerEnum = ESongEditorLayer.ForcedAlignment;
        editorNoteDisplayer.ClearNotesInLayer(layerEnum);
        songEditorLayerManager.ClearEnumLayer(layerEnum);

        // Add notes to layer
        notes.ForEach(note =>
        {
            songEditorLayerManager.AddNoteToEnumLayer(layerEnum, note);
            note.IsEditable = songEditorLayerManager.IsLayerEditable(
                songEditorLayerManager.GetEnumLayer(layerEnum));
        });

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }

        return forcedAlignmentResult;
    }

    public Awaitable<ForcedAlignmentResult> RunForcedAlignment(List<Note> selectedNotes, bool notify)
    {
        // TODO: Implement, do force alignment with note lyrics, then move notes accordingly
        return null;
    }
}
