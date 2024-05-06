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

    public void SetTextToAnalyzedSpeech(List<Note> selectedNotes, ESongEditorSamplesSource samplesSource, bool notify)
    {
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }

        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
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

        SpeechRecognitionUtils.GetOrCreateSpeechRecognizerAsObservable(speechRecognitionParameters, speechRecognitionJob)
            .SelectMany(speechRecognizer =>
            {
                speechRecognitionJob.SetStatus(EJobStatus.Running);

                float[] monoAudioSamples =
                    AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, audioClip, minBeat, lengthInBeats, true);

                return SpeechRecognitionUtils.DoSpeechRecognitionAsObservable(
                        monoAudioSamples,
                        0,
                        monoAudioSamples.Length - 1,
                        audioClip.frequency,
                        cancellationTokenSource.Token,
                        onProgress,
                        speechRecognizer,
                        false)
                    // Execute on Background thread
                    .SubscribeOn(Scheduler.ThreadPool)
                    // Notify on Main thread
                    .ObserveOnMainThread();
            })
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Set text to analyzed speech failed: {ex.Message}");
                speechRecognitionJob.SetResult(EJobResult.Error);
            })
            .Subscribe(speechRecognitionResult =>
            {
                speechRecognitionJob.SetResult(EJobResult.Ok);
                SpeechRecognitionUtils.MapSpeechRecognitionResultTextToNotes(songMeta, speechRecognitionResult.Words,
                    selectedNotes, minBeat);
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
                }
            });
    }

    public void CreateNotesFromSpeechRecognition(
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
        CreateNotesFromSpeechRecognitionAsObservable(
                monoAudioSamples,
                startIndex,
                endIndex,
                sampleRate,
                spaceBetweenNotesInMillis,
                notify,
                speechRecognitionParameters,
                continuous,
                offsetInBeats)
            // Subscribe to trigger observable
            .Subscribe(createdNotes => Debug.Log($"Created notes from speech recognition: {createdNotes.Count}"));
    }

    public IObservable<List<Note>> CreateNotesFromSpeechRecognitionAsObservable(
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
            return Observable.Empty<List<Note>>();
        }

        Hyphenator hyphenator = settings.SongEditorSettings.SplitSyllablesAfterSpeechRecognition
            ? SettingsUtils.CreateHyphenator(settings)
            : null;

        return SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsObservable(
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
                settings.SongEditorSettings.SpaceBetweenNotesInMillis)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Create notes from speech recognition failed: {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason, "reason", ex.Message));
            })
            .Select(createdNotes =>
            {
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
            });
    }

    public void CreateNotesFromSpeechRecognition(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous)
    {
        CreateNotesFromSpeechRecognitionAsObservable(startBeat,
                lengthInBeats,
                speechRecognitionSampleSource,
                spaceBetweenNotesInMillis,
                notify,
                speechRecognitionParameters,
                continuous)
            // Subscribe to trigger observable
            .Subscribe(createdNotes => Debug.Log("Created notes from speech recognition: " + createdNotes.Count));
    }

    private IObservable<List<Note>> CreateNotesFromSpeechRecognitionAsObservable(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        bool continuous)
    {
        AudioClip audioClip = GetAudioClip(speechRecognitionSampleSource);
        if (audioClip == null
            || lengthInBeats <= 0)
        {
            return Observable.Empty<List<Note>>();
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

        return SpeechRecognitionUtils.CreateNotesFromSpeechRecognitionAsObservable(
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
                settings.SongEditorSettings.SpaceBetweenNotesInMillis)
            .SubscribeOn(Scheduler.MainThread)
            .Select(createdNotes =>
            {
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
            });
    }

    public SpeechRecognitionParameters CreateSpeechRecognizerParameters()
    {
        return new SpeechRecognitionParameters(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);
    }
}
