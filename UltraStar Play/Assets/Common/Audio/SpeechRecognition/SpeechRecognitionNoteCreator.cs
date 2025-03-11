using System;
using System.Collections.Generic;
using System.Linq;
using NHyphenator;
using UniInject;
using UnityEngine;

public class SpeechRecognitionNoteCreator : AbstractSingletonBehaviour, INeedInjection
{
    public static SpeechRecognitionNoteCreator Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SpeechRecognitionNoteCreator>();

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private SpeechRecognizerProvider speechRecognizerProvider;

    protected override object GetInstance()
    {
        return Instance;
    }

    public Job<List<Note>> CreateNotesFromSpeechRecognitionJob(
        SpeechRecognitionInputSamples samples,
        SpeechRecognizerConfig speechRecognizerConfig,
        int midiNote,
        SongMeta songMeta,
        int offsetInBeats,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes)
    {
        Job<List<Note>> job = new(Translation.Of("Create notes from speech recognition"));
        JobManager.Instance.AddJob(job);

        job.SetAwaitable(async () =>
        {
            SpeechRecognizer speechRecognizer = await speechRecognizerProvider.GetSpeechRecognizerJob(speechRecognizerConfig).GetResultAsync();

            await Awaitable.BackgroundThreadAsync();
            SpeechRecognitionResult speechRecognitionResult = await speechRecognitionManager.ProcessSongMetaJob(
                samples,
                speechRecognizer)
                .GetResultAsync();

            await Awaitable.MainThreadAsync();
            List<Note> createdNotes = CreateNotesFromSpeechRecognitionResult(speechRecognitionResult, songMeta, offsetInBeats, midiNote, hyphenator, spaceInMillisBetweenNotes);

            return createdNotes;
        });

        return job;
    }

    private static List<Note> CreateNotesFromSpeechRecognitionResult(
        SpeechRecognitionResult speechRecognitionResult,
        SongMeta songMeta,
        int offsetInBeats,
        int midiNote,
        Hyphenator hyphenator,
        int spaceInMillisBetweenNotes)
    {
        if (speechRecognitionResult == null
            || speechRecognitionResult.Words.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        double beatsPerSeconds = SongMetaBpmUtils.BeatsPerSecond(songMeta);
        List<Note> createdNotes = speechRecognitionResult.Words.Select(resultEntry =>
        {
            int noteStartInBeats = offsetInBeats + (int)(resultEntry.Start.TotalSeconds * beatsPerSeconds);
            int noteEndInBeats = offsetInBeats + (int)(resultEntry.End.TotalSeconds * beatsPerSeconds);
            if (noteEndInBeats <= noteStartInBeats)
            {
                noteEndInBeats = noteStartInBeats + 1;
            }
            int noteLengthInBeats = noteEndInBeats - noteStartInBeats;
            string text = resultEntry.Text;
            Note createdNote = new(ENoteType.Normal, noteStartInBeats, noteLengthInBeats, MidiUtils.GetUltraStarTxtPitch(midiNote), text);
            return createdNote;
        }).ToList();

        // Shorten new notes left and right to give a little space
        SpaceBetweenNotesUtils.ShortenNotesByMillis(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

        // Split syllables if hyphenation is enabled
        if (hyphenator != null)
        {
            Dictionary<Note,List<Note>> noteToNotesAfterSplit = HyphenateNotesUtils.HypenateNotes(songMeta, createdNotes, hyphenator);
            noteToNotesAfterSplit.ForEach(entry =>
            {
                Note note = entry.Key;
                List<Note> notesAfterSplit = entry.Value;
                List<Note> newNotes = new List<Note>(notesAfterSplit);
                newNotes.Remove(note);
                createdNotes.AddRange(newNotes);
            });
        }

        // Shorten new notes left and right to give a little space
        if (spaceInMillisBetweenNotes > 0)
        {
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, spaceInMillisBetweenNotes, songMeta);
        }

        return createdNotes;
    }
}
