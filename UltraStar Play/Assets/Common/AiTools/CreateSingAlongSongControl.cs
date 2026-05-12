using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateSingAlongSongControl : INeedInjection
{
    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private PitchDetectionManager pitchDetectionManager;

    [Inject]
    private ForcedAlignmentManager forcedAlignmentManager;

    [Inject]
    private SpeechRecognitionNoteCreator speechRecognitionNoteCreator;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    private IJob lastProcessSongJob;

    private readonly Subject<SongMeta> createdSingAlongVersionEventStream = new();
    public IObservable<SongMeta> CreatedSingAlongVersionEventStream => createdSingAlongVersionEventStream;

    public async void CreateSingAlongSong(SongMeta songMeta, bool saveSongFile)
    {
        await CreateSingAlongSongAsync(songMeta, saveSongFile);
    }

    public async Awaitable<SongMeta> CreateSingAlongSongAsync(SongMeta songMeta, bool saveSongFile)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException(nameof(songMeta));
        }

        if (lastProcessSongJob != null
            && lastProcessSongJob.Result.Value == EJobResult.Pending)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            throw new JobAlreadyRunningException("Already creating sing along data for another song");
        }
        Debug.Log($"Creating sing-along data for song '{songMeta.GetArtistDashTitle()}'");

        // Create and run job
        IJob parentJob = SingAlongDataJobPipeline(songMeta, saveSongFile);
        jobManager.AddJob(parentJob);
        await parentJob.RunAsync();

        // Save
        if (saveSongFile)
        {
            SaveAndReloadSong(songMeta);
        }

        return songMeta;
    }

    private IJob SingAlongDataJobPipeline(SongMeta songMeta, bool saveSongFile)
    {
        IJob parentJob = new Job<VoidEvent>(Translation.Get(R.Messages.job_createSingAlongDataWithName,
            "name", Path.GetFileName(songMeta.Audio)));
        lastProcessSongJob = parentJob;

        PipelineData pipelineData = new();

        // Run vocals isolation
        parentJob.AddChildJob(AudioSeparationJob(songMeta, saveSongFile));

        // Run speech recognition or forced alignment
        if (!nonPersistentSettings.ForcedAlignmentLyrics.IsNullOrEmpty())
        {
            parentJob.AddChildJob(ForcedAlignmentJob(songMeta, pipelineData));
        }
        else
        {
            parentJob.AddChildJob(SpeechRecognitionJob(songMeta, pipelineData));
        }

        // Run pitch detection
        Job<VoidEvent> pitchDetectionJob = PitchDetectionJob(songMeta, pipelineData);
        parentJob.AddChildJob(pitchDetectionJob);

        return parentJob;
    }

    private Job<VoidEvent> PitchDetectionJob(SongMeta songMeta, PipelineData pipelineData)
    {
        Job<VoidEvent> pitchDetectionJob = new(Translation.Of("Pitch detection"));
        pitchDetectionJob.SetAwaitable(async () =>
        {
            PitchDetectionResult pitchDetectionResult = await pitchDetectionManager.ProcessSongMetaJob(songMeta).GetResultAsync();

            // Move notes of first player to detected pitch
            MoveNotesToDetectedPitch(songMeta, pipelineData.CreatedNotes, pitchDetectionResult);

            return VoidEvent.instance;
        });
        return pitchDetectionJob;
    }

    private Job<VoidEvent> SpeechRecognitionJob(SongMeta songMeta, PipelineData pipelineData)
    {
        Job<VoidEvent> speechRecognitionJob = new(Translation.Of("Speech recognition"));
        speechRecognitionJob.SetAwaitable(async () =>
        {
            // Load vocals audio
            AudioClip vocalsAudioClip = await AudioManager.LoadAudioClipFromUriAsync(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
            int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * SongMetaBpmUtils.BeatsPerSecond(songMeta));

            float[] monoAudioSamples = SongMetaAudioSampleUtils.GetMonoSamples(songMeta, vocalsAudioClip, 0, lengthInBeats);

            pipelineData.CreatedNotes = await speechRecognitionNoteCreator.CreateNotesFromSpeechRecognitionJob(
                new CreateNotesFromSpeechRecognitionConfig
                {
                    InputSamples = new SpeechRecognitionInputSamples(monoAudioSamples, 0, monoAudioSamples.Length - 1, vocalsAudioClip.frequency),
                    MidiNote = settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                    SongMeta = songMeta,
                    OffsetInBeats = 0,
                    Hyphenator = SettingsUtils.CreateHyphenator(settings),
                    SpaceInMillisBetweenNotes = settings.SongEditorSettings.SpaceBetweenNotesInMillis,
                })
                .GetResultAsync();

            // Split created notes into sentences and assign to first player
            AssignNotesToFirstPlayer(songMeta, pipelineData.CreatedNotes);

            // Add Space between notes
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(pipelineData.CreatedNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

            return VoidEvent.instance;
        });
        return speechRecognitionJob;
    }

    private Job<VoidEvent> ForcedAlignmentJob(SongMeta songMeta, PipelineData pipelineData)
    {
        Job<VoidEvent> forcedAlignmentJob = new(Translation.Of("Forced alignment"));
        forcedAlignmentJob.SetAwaitable(async () =>
        {
            // Load vocals audio
            AudioClip vocalsAudioClip = await AudioManager.LoadAudioClipFromUriAsync(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
            int lengthInBeats = (int)Math.Floor(vocalsAudioClip.length * SongMetaBpmUtils.BeatsPerSecond(songMeta));

            float[] monoAudioSamples = SongMetaAudioSampleUtils.GetMonoSamples(songMeta, vocalsAudioClip, 0, lengthInBeats);

            ForcedAlignmentInput forcedAlignmentInput = new ForcedAlignmentInput(
                nonPersistentSettings.ForcedAlignmentLyrics,
                monoAudioSamples,
                0,
                monoAudioSamples.Length - 1,
                vocalsAudioClip.frequency);

            ForcedAlignmentResult forcedAlignmentResult = await forcedAlignmentManager.ProcessSongMetaJob(
                    songMeta,
                    forcedAlignmentInput)
                .GetResultAsync();

            if (forcedAlignmentResult == null || forcedAlignmentResult.Words.IsNullOrEmpty())
            {
                return VoidEvent.instance;
            }

            pipelineData.CreatedNotes = CreateNotesFromForcedAlignmentResult(songMeta, forcedAlignmentResult);

            // Split created notes into sentences and assign to first player
            AssignNotesToFirstPlayer(songMeta, pipelineData.CreatedNotes);

            // Add Space between notes
            SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(pipelineData.CreatedNotes, SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis, songMeta);

            return VoidEvent.instance;
        });
        return forcedAlignmentJob;
    }

    private List<Note> CreateNotesFromForcedAlignmentResult(SongMeta songMeta, ForcedAlignmentResult forcedAlignmentResult)
    {
        return forcedAlignmentResult.Words
            .Select(wordTimestamp =>
            {
                double startInMillis = wordTimestamp.StartTime * 1000;
                double endInMillis = wordTimestamp.EndTime * 1000;
                int startBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, startInMillis);
                int endBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, endInMillis);
                int lengthInBeats = Math.Max(1, endBeat - startBeat);
                return new Note(
                    ENoteType.Normal,
                    startBeat,
                    lengthInBeats,
                    MidiUtils.GetUltraStarTxtPitch(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
                    wordTimestamp.Word);
            })
            .ToList();
    }

    private Job<VoidEvent> AudioSeparationJob(SongMeta songMeta, bool saveSongFile)
    {
        Job<VoidEvent> audioSeparationJob = new(Translation.Of("Audio separation"));
        audioSeparationJob.SetAwaitable(async () =>
        {
            await audioSeparationManager.ProcessSongMetaJob(songMeta, saveSongFile).GetResultAsync();
            return VoidEvent.instance;
        });
        return audioSeparationJob;
    }

    private static void MoveNotesToDetectedPitch(SongMeta songMeta, List<Note> createdNotes, PitchDetectionResult pitchDetectionResult)
    {
        try
        {
            PitchDetectionNoteMover.MoveNotesToDetectedPitch(
                songMeta,
                createdNotes,
                pitchDetectionResult);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to move notes to detected pitch");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
    }

    private void SaveAndReloadSong(SongMeta songMeta)
    {
        try
        {
            songMetaManager.SaveSong(songMeta, true);
            songMetaManager.ReloadSong(songMeta);

            createdSingAlongVersionEventStream.OnNext(songMeta);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to save song with sing-along data");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", ex.Message));
        }
    }

    private static void AssignNotesToFirstPlayer(SongMeta songMeta, List<Note> createdNotes)
    {
        RemoveAllNotes(songMeta);
        List<List<Note>> noteBatches = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, createdNotes);
        noteBatches.ForEach(noteBatch => MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, noteBatch, EVoiceId.P1));
    }

    private static void RemoveAllNotes(SongMeta songMeta)
    {
        songMeta.Voices.ForEach(voice =>
            voice.Sentences.ToList().ForEach(sentence => voice.RemoveSentence(sentence)));
    }

    private class PipelineData
    {
        public List<Note> CreatedNotes { get; set; }
    }
}
