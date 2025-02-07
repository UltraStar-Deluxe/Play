using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NHyphenator;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionAction : AbstractAudioClipAction
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        speechRecognitionProcessCount = 0;
    }

    private static object lockObject = new();
    private static int speechRecognitionProcessCount;

    [Inject] private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject] private SongAudioPlayer songAudioPlayer;

    [Inject] private SpeechRecognitionManager speechRecognitionManager;

    [Inject] private SongEditorLayerManager songEditorLayerManager;

    [Inject] private EditorNoteDisplayer editorNoteDisplayer;

    [Inject] private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject] private JobManager jobManager;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    public async void SetTextToAnalyzedSpeech(List<Note> selectedNotes, ESongEditorSamplesSource samplesSource, bool notify)
    {
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }

        AudioClip audioClip = await GetAudioClip(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
        if (audioClip == null)
        {
            return;
        }

        int minBeat = SongMetaUtils.MinBeat(selectedNotes);
        int lengthInBeats = SongMetaUtils.LengthInBeats(selectedNotes);
        double lengthInMillis = SongMetaBpmUtils.MillisPerBeat(songMeta) * lengthInBeats;
        Job speechRecognitionJob = new(Translation.Get(R.Messages.job_speechRecognition));
        jobManager.AddJob(speechRecognitionJob);
        speechRecognitionJob.EstimatedTotalDurationInMillis =
            SpeechRecognitionUtils.GetEstimatedSpeechRecognitionDurationInMillis(lengthInMillis);

        CancellationTokenSource cancellationTokenSource = new();
        speechRecognitionJob.OnCancel = () => cancellationTokenSource.Cancel();

        Action<double> onProgress = progressInPercent =>
            speechRecognitionJob.EstimatedCurrentProgressInPercent = progressInPercent;

        SpeechRecognitionParameters speechRecognitionParameters = CreateSpeechRecognizerParameters();

        try
        {
            SpeechRecognizer speechRecognizer = await SpeechRecognitionUtils.GetOrCreateSpeechRecognizerAsync(speechRecognitionParameters, speechRecognitionJob);
            speechRecognitionJob.SetStatus(EJobStatus.Running);

            float[] monoAudioSamples =
                AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, audioClip, minBeat, lengthInBeats, true);

            await Awaitable.BackgroundThreadAsync();
            SpeechRecognitionResult speechRecognitionResult = await SpeechRecognitionUtils.DoSpeechRecognitionAsync(
                monoAudioSamples,
                0,
                monoAudioSamples.Length - 1,
                audioClip.frequency,
                cancellationTokenSource.Token,
                onProgress,
                speechRecognizer,
                false);

            await Awaitable.MainThreadAsync();

            speechRecognitionJob.SetResult(EJobResult.Ok);
            SpeechRecognitionUtils.MapSpeechRecognitionResultTextToNotes(songMeta, speechRecognitionResult.Words, selectedNotes, minBeat);
            if (notify)
            {
                songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
            }
        }
        catch (Exception ex)
        {
            speechRecognitionJob?.SetResult(EJobResult.Error);
            throw new SpeechRecognitionException("Set text to analyzed speech failed", ex);
        }
    }

    public async void CreateNotesFromSpeechRecognition(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous,
        int offsetInBeats)
    {
        await CreateNotesFromSpeechRecognitionAsync(
            monoAudioSamples,
            startIndex,
            endIndex,
            sampleRate,
            spaceBetweenNotesInMillis,
            notify,
            speechRecognitionParameters,
            continuous,
            offsetInBeats);
    }

    public async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionAsync(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous,
        int offsetInBeats)
    {
        int lengthInSamples = endIndex - startIndex;
        if (monoAudioSamples.IsNullOrEmpty()
            || lengthInSamples <= 0)
        {
            return new List<Note>();
        }

        Hyphenator hyphenator = settings.SongEditorSettings.SplitSyllablesAfterSpeechRecognition
            ? SettingsUtils.CreateHyphenator(settings)
            : null;

        try
        {
            List<Note> createdNotes = await SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsync(
                    monoAudioSamples,
                    startIndex,
                    endIndex,
                    sampleRate,
                    speechRecognitionParameters,
                    null,
                    continuous,
                    settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                    songMeta,
                    offsetInBeats,
                    hyphenator,
                    settings.SongEditorSettings.SpaceBetweenNotesInMillis);

            createdNotes.ForEach(createdNote =>
            {
                createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.SpeechRecognition);
                songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.SpeechRecognition, createdNote);
            });

            if (spaceBetweenNotesInMillis > 0)
            {
                spaceBetweenNotesAction.Execute(songMeta, createdNotes, spaceBetweenNotesInMillis);
            }

            if (notify)
            {
                songMetaChangeEventStream.OnNext(new NotesChangedEvent());
            }

            return createdNotes;
        }
        catch (Exception ex)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason, "reason", ex.Message));
            throw new SpeechRecognitionException($"Create notes from speech recognition failed", ex);
        }
    }

    public async void CreateNotesFromSpeechRecognition(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous)
    {
        await CreateNotesFromSpeechRecognitionAsync(startBeat,
            lengthInBeats,
            speechRecognitionSampleSource,
            spaceBetweenNotesInMillis,
            notify,
            speechRecognitionParameters,
            continuous);
    }

    private async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionAsync(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous)
    {
        AudioClip audioClip = await GetAudioClip(speechRecognitionSampleSource);
        if (audioClip == null
            || lengthInBeats <= 0)
        {
            return new List<Note>();
        }

        // Remove old notes
        songEditorLayerManager.GetEnumLayerNotes(ESongEditorLayer.SpeechRecognition)
            .Where(oldNote =>
                oldNote.StartBeat >= startBeat && oldNote.EndBeat <= startBeat + lengthInBeats)
            .ForEach(oldNote =>
            {
                editorNoteDisplayer.RemoveNoteControl(oldNote);
                songEditorLayerManager.RemoveNoteFromAllEnumLayers(oldNote);
            });

        float[] monoAudioSamples = AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, audioClip, startBeat, lengthInBeats, true);

        Hyphenator hyphenator = settings.SongEditorSettings.SplitSyllablesAfterSpeechRecognition
            ? SettingsUtils.CreateHyphenator(settings)
            : null;

        List<Note> createdNotes = await SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsync(
                monoAudioSamples,
                0,
                monoAudioSamples.Length - 1,
                audioClip.frequency,
                speechRecognitionParameters,
                JobManager.CreateAndAddJob(Translation.Get(R.Messages.job_speechRecognition)),
                continuous,
                settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                songMeta,
                startBeat,
                hyphenator,
                settings.SongEditorSettings.SpaceBetweenNotesInMillis);

        createdNotes.ForEach(createdNote =>
        {
            createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.SpeechRecognition);
            songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.SpeechRecognition, createdNote);
        });

        if (spaceBetweenNotesInMillis > 0)
        {
            spaceBetweenNotesAction.Execute(songMeta, createdNotes, spaceBetweenNotesInMillis);
        }

        if (notify)
        {
            songMetaChangeEventStream.OnNext(new NotesChangedEvent());
        }

        return createdNotes;
    }

    public SpeechRecognitionParameters CreateSpeechRecognizerParameters()
    {
        return new SpeechRecognitionParameters(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);
    }
}
