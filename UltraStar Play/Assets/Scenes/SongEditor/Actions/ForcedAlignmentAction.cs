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

    [Inject] private NoteAreaControl noteAreaControl;

    [Inject] private NoteHyphenator noteHyphenator;

    public async Awaitable<ForcedAlignmentResult> MoveNotesViaForcedAlignmentInSelection(
        List<Note> notes,
        int startBeat,
        int lengthInBeats,
        bool notify)
    {
        if (notes.IsNullOrEmpty())
        {
            return null;
        }

        List<Note> sortedNotes = notes
            .OrderBy(it => it.StartBeat)
            .ToList();

        string lyrics = ForcedAlignmentUtils.GetLyricsFromNotes(sortedNotes);

        if (lyrics.IsNullOrEmpty())
        {
            return null;
        }

        ForcedAlignmentInput forcedAlignmentInput = await GetForcedAlignmentInput(lyrics, startBeat, lengthInBeats);
        ForcedAlignmentResult forcedAlignmentResult = await RunForcedAlignmentInternal(forcedAlignmentInput);
        if (forcedAlignmentResult == null || forcedAlignmentResult.Words.IsNullOrEmpty())
        {
            return forcedAlignmentResult;
        }

        ForcedAlignmentUtils.MoveNotesToForcedAlignmentResult(songMeta, sortedNotes, forcedAlignmentResult, startBeat);

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
        
        return forcedAlignmentResult;
    }

    public async Awaitable<List<Note>> CreateNotesViaForcedAlignmentInSelection(
        string lyrics,
        int startBeat,
        int lengthInBeats,
        bool notify)
    {
        if (lengthInBeats <= 0)
        {
            return new List<Note>();
        }

        ForcedAlignmentInput forcedAlignmentInput = await GetForcedAlignmentInput(lyrics, startBeat, lengthInBeats);
        ForcedAlignmentResult forcedAlignmentResult = await RunForcedAlignmentInternal(forcedAlignmentInput);
        if (forcedAlignmentResult == null || forcedAlignmentResult.Words.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        List<Note> notes = CreateNotesFromForcedAlignmentResult(forcedAlignmentResult, startBeat);

        if (notify)
        {
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }
        
        noteAreaControl.ScrollIntoView(notes);
        
        return notes;
    }

    private async Awaitable<ForcedAlignmentResult> RunForcedAlignmentInternal(ForcedAlignmentInput forcedAlignmentInput)
    {
        ForcedAlignmentResult forcedAlignmentResult = await forcedAlignmentManager.ProcessSongMetaJob(
            songMeta,
            forcedAlignmentInput)
            .GetResultAsync();

        return forcedAlignmentResult;
    }
    
    private List<Note> CreateNotesFromForcedAlignmentResult(ForcedAlignmentResult forcedAlignmentResult, int offsetInBeats)
    {
        List<Note> notes = ForcedAlignmentUtils.CreateNotesFromForcedAlignmentResult(forcedAlignmentResult, songMeta, settings, noteHyphenator, offsetInBeats);

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

        return notes;
    }
    

    private async Awaitable<ForcedAlignmentInput> GetForcedAlignmentInput(
        string lyrics,
        int startBeat,
        int lengthInBeats)
    {
        AudioClip audioClip = await GetAudioClip(settings.SongEditorSettings.AiSamplesSource);
        if (audioClip == null)
        {
            return new ForcedAlignmentInput(lyrics, Array.Empty<float>(), 0, 0, 0);
        }
        
        float[] monoAudioSamples = SongMetaAudioSampleUtils.GetMonoSamples(songMeta, audioClip, startBeat, lengthInBeats);
        return new ForcedAlignmentInput(lyrics, monoAudioSamples, 0, monoAudioSamples.Length - 1, audioClip.frequency);
    }
    
    private async Awaitable<ForcedAlignmentInput> GetForcedAlignmentInput(string lyrics)
    {
        AudioClip audioClip = await GetAudioClip(settings.SongEditorSettings.AiSamplesSource);
        if (audioClip == null)
        {
            return new ForcedAlignmentInput(lyrics, Array.Empty<float>(), 0, 0, 0);
        }

        int lengthInBeats = (int)SongMetaBpmUtils.MillisToBeats(songMeta, audioClip.length * 1000.0);
        return await GetForcedAlignmentInput(lyrics, 0, lengthInBeats);
    }
}
