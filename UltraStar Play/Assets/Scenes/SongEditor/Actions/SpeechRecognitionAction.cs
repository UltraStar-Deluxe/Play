using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Vosk;

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
        double lengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta) * lengthInBeats;
        Job speechRecognitionJob = new("Speech recognition");
        jobManager.AddJob(speechRecognitionJob);
        speechRecognitionJob.EstimatedTotalDurationInMillis =
            SpeechRecognitionUtils.GetEstimatedSpeechRecognitionDurationInMillis(lengthInMillis);

        CancellationTokenSource cancellationTokenSource = new();
        speechRecognitionJob.OnCancel = () => cancellationTokenSource.Cancel();

        Action<double> onProgress = progressInPercent =>
            speechRecognitionJob.EstimatedCurrentProgressInPercent = progressInPercent;

        SpeechRecognitionParameters speechRecognitionParameters = CreateSpeechRecognizerParameters(samplesSource);

        VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);

        IObservable<object> loadSpeechRecognitionModelObservable =
            SpeechRecognitionUtils.LoadSpeechRecognitionModel(speechRecognitionParameters.ModelPath,
                speechRecognitionJob);
        loadSpeechRecognitionModelObservable.Subscribe(_ =>
        {
            speechRecognitionJob.SetStatus(EJobStatus.Running);

            float[] monoAudioSamples =
                AudioUtils.GetSamplesOfBeatRangeFromAudioClip(songMeta, audioClip, minBeat, lengthInBeats, true);

            SpeechRecognitionUtils.DoSpeechRecognitionAsObservable(
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
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogError(ex);
                    speechRecognitionJob.SetResult(EJobResult.Error);
                })
                .Subscribe(voskResultJson =>
                {
                    speechRecognitionJob.SetResult(EJobResult.Ok);
                    SpeechRecognitionUtils.MapSpeechRecognitionResultTextToNotes(songMeta, voskResultJson.result,
                        selectedNotes, minBeat);
                    if (notify)
                    {
                        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
                    }
                });
        });
    }

    public IObservable<List<Note>> CreateNotesFromSpeechRecognition(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        int spaceBetweenNotesInBeats,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        VoskRecognizer speechRecognizer,
        bool continuous,
        int offsetInBeats)
    {
        int lengthInSamples = endIndex - startIndex;
        if (monoAudioSamples.IsNullOrEmpty()
            || lengthInSamples <= 0)
        {
            return Observable.Empty<List<Note>>();
        }

        IObservable<List<Note>> createNotesObservable = SpeechRecognitionUtils.CreateNotesFromSpeechRecognition(
                monoAudioSamples,
                startIndex,
                endIndex,
                sampleRate,
                speechRecognitionParameters,
                null,
                speechRecognizer,
                continuous,
                settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                songMeta,
                offsetInBeats)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                UiManager.CreateNotification(ex.Message);
            });

        createNotesObservable.Subscribe(createdNotes =>
        {
            createdNotes.ForEach(createdNote =>
            {
                createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.SpeechRecognition);
                songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.SpeechRecognition, createdNote);
            });

            if (spaceBetweenNotesInBeats > 0)
            {
                spaceBetweenNotesAction.Execute(createdNotes, spaceBetweenNotesInBeats);
            }

            if (notify)
            {
                songMetaChangeEventStream.OnNext(new NotesChangedEvent());
            }
        });
        return createNotesObservable;
    }
    
    public IObservable<List<Note>> CreateNotesFromSpeechRecognition(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInBeats,
        bool notify,
        SpeechRecognitionParameters speechRecognitionParameters,
        VoskRecognizer speechRecognizer,
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

        IObservable<List<Note>> createNotesObservable = SpeechRecognitionUtils.CreateNotesFromSpeechRecognition(
                monoAudioSamples,
                0,
                monoAudioSamples.Length - 1,
                audioClip.frequency,
                speechRecognitionParameters,
                JobManager.CreateAndAddJob("Speech Recognition"),
                speechRecognizer,
                continuous,
                settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                songMeta,
                startBeat)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogError(ex);
                UiManager.CreateNotification(ex.Message);
            });

        createNotesObservable.Subscribe(createdNotes =>
        {
            createdNotes.ForEach(createdNote =>
            {
                createdNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.SpeechRecognition);
                songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.SpeechRecognition, createdNote);
            });

            if (spaceBetweenNotesInBeats > 0)
            {
                spaceBetweenNotesAction.Execute(createdNotes, spaceBetweenNotesInBeats);
            }

            if (notify)
            {
                songMetaChangeEventStream.OnNext(new NotesChangedEvent());
            }
        });
        return createNotesObservable;
    }
    
    public SpeechRecognitionParameters CreateSpeechRecognizerParameters(ESongEditorSamplesSource samplesSource)
    {
        AudioClip audioClip = GetAudioClip(samplesSource);
        return new SpeechRecognitionParameters(
            audioClip.frequency,
            GetSpeechRecognitionModelPath(),
            SpeechRecognitionUtils.GetSpeechRecognitionPhrases(settings.SongEditorSettings.SpeechRecognitionPhrases));
    }

    private string GetSpeechRecognitionModelPath()
    {
        return speechRecognitionModelPathTextField.text;
    }
}
