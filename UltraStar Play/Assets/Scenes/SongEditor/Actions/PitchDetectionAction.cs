using System.Collections.Generic;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionAction : AbstractAudioClipAction
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private PitchDetectionManager pitchDetectionManager;

    [Inject]
    private SongEditorMidiFileImporter songEditorMidiFileImporter;

    public async Awaitable<PitchDetectionResult> CreateNotesUsingAi(bool notify)
    {
        PitchDetectionResult pitchDetectionResult = await pitchDetectionManager.ProcessSongMetaJob(songMeta).GetResultAsync();
        List<Note> notes = PitchDetectionResultMapper.ToSongMetaNotes(songMeta, pitchDetectionResult);
        
        // Remove old notes
        editorNoteDisplayer.ClearNotesInLayer(ESongEditorLayer.PitchDetection);
        songEditorLayerManager.ClearEnumLayer(ESongEditorLayer.PitchDetection);
        
        // Add notes to layer
        notes.ForEach(note =>
        {
            songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.PitchDetection, note);
            note.IsEditable = songEditorLayerManager.IsLayerEditable(songEditorLayerManager.GetEnumLayer(ESongEditorLayer.PitchDetection));
        });

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }

        return pitchDetectionResult;
    }
}
