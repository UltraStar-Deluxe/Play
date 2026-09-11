using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniInject;
using UnityEngine;

public class SpeechRecognitionNoteCreator : AbstractSingletonBehaviour, INeedInjection
{
    public static SpeechRecognitionNoteCreator Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SpeechRecognitionNoteCreator>();

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private NoteHyphenator noteHyphenator;

    [Inject]
    private ForcedAlignmentManager forcedAlignmentManager;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    public Job<List<Note>> CreateNotesFromSpeechRecognitionJob(CreateNotesFromSpeechRecognitionConfig config)
    {
        Job<List<Note>> job = new(Translation.Of("Create notes from speech recognition"));
        JobManager.Instance.AddJob(job);

        job.SetAwaitable(async () =>
        {
            SpeechRecognitionResult speechRecognitionResult = await speechRecognitionManager.ProcessSongMetaJob(
                config.InputSamples)
                .GetResultAsync();

            List<Note> createdNotes = await CreateNotesFromSpeechRecognitionResult(
                speechRecognitionResult,
                config);

            return createdNotes;
        });

        return job;
    }

    private async Awaitable PerformForcedAlignment(CreateNotesFromSpeechRecognitionConfig config, List<Note> createdNotes)
    {
        if (!settings.SongEditorSettings.ForcedAlignmentAfterSpeechRecognition)
        {
            return;
        }
        
        string lyrics = ForcedAlignmentUtils.GetLyricsFromNotes(createdNotes);
        if (!lyrics.IsNullOrEmpty())
        {
            ForcedAlignmentInput forcedAlignmentInput = new ForcedAlignmentInput(
                lyrics,
                config.InputSamples.MonoSamples,
                config.InputSamples.StartIndex,
                config.InputSamples.EndIndex,
                config.InputSamples.SampleRate);

            ForcedAlignmentResult forcedAlignmentResult = await forcedAlignmentManager.ProcessSongMetaJob(
                    config.SongMeta,
                    forcedAlignmentInput)
                .GetResultAsync();

            if (forcedAlignmentResult != null && !forcedAlignmentResult.Words.IsNullOrEmpty())
            {
                ForcedAlignmentUtils.MoveNotesToForcedAlignmentResult(
                    config.SongMeta,
                    createdNotes.OrderBy(it => it.StartBeat).ToList(),
                    forcedAlignmentResult,
                    config.OffsetInBeats);
            }
        }
    }

    private async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionResult(
        SpeechRecognitionResult speechRecognitionResult,
        CreateNotesFromSpeechRecognitionConfig config)
    {
        if (speechRecognitionResult == null
            || speechRecognitionResult.Words.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        double beatsPerSeconds = SongMetaBpmUtils.BeatsPerSecond(config.SongMeta);
        List<Note> createdNotes = speechRecognitionResult.Words.Select(resultEntry =>
        {
            int noteStartInBeats = config.OffsetInBeats + (int)(resultEntry.Start.TotalSeconds * beatsPerSeconds);
            int noteEndInBeats = config.OffsetInBeats + (int)(resultEntry.End.TotalSeconds * beatsPerSeconds);
            if (noteEndInBeats <= noteStartInBeats)
            {
                noteEndInBeats = noteStartInBeats + 1;
            }
            int noteLengthInBeats = noteEndInBeats - noteStartInBeats;
            
            string text = resultEntry.Text;
            if (!text.IsNullOrEmpty())
            {
                text = text.Trim() + " ";
            }
            
            Note createdNote = new(ENoteType.Normal, noteStartInBeats, noteLengthInBeats, MidiUtils.GetUltraStarTxtPitch(config.MidiNote), text);
            return createdNote;
        }).ToList();

        try
        {
            await PerformForcedAlignment(config, createdNotes);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to perform forced alignment to optimize note positions of speech recognition result.");
        }

        // Shorten new notes left and right to give a little space
        SpaceBetweenNotesUtils.ShortenNotesByMillis(createdNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, config.SongMeta);

        // Split syllables if hyphenation is enabled
        if (config.Hyphenator != null)
        {
            HyphenationUtils.SplitNotesByHyphenation(settings, noteHyphenator, createdNotes);
        }

        // Shorten new notes left and right to give a little space
        if (config.SpaceInMillisBetweenNotes > 0)
        {
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(createdNotes, config.SpaceInMillisBetweenNotes, config.SongMeta);
        }

        return createdNotes;
    }
}
