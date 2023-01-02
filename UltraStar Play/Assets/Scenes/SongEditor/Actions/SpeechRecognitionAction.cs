using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEditor.Search;
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

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject]
    private JobManager jobManager;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    public void SetTextToAnalyzedSpeech(List<Note> selectedNotes, bool notify)
    {
        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
        if (audioClip == null)
        {
            return;
        }

        int minBeat = SongMetaUtils.MinBeat(selectedNotes);
        int lengthInBeats = SongMetaUtils.LengthInBeats(selectedNotes);
        Job speechRecognitionJob = new("Speech recognition to set lyrics");
        jobManager.AddJob(speechRecognitionJob);
        speechRecognitionJob.EstimatedTotalDurationInMillis = SpeechRecognitionUtils.GetEstimatedSpeechRecognitionDurationInMillis(songMeta, lengthInBeats);

        CancellationTokenSource cancellationTokenSource = new();
        speechRecognitionJob.OnCancel = () => cancellationTokenSource.Cancel();

        Action<double> onProgress = progressInPercent => speechRecognitionJob.EstimatedCurrentProgressInPercent = progressInPercent;

        SpeechRecognitionParameters speechRecognizerParameters = CreateSpeechRecognizerParameters();
        IObservable<object> loadSpeechRecognitionModelObservable = SpeechRecognitionUtils.LoadSpeechRecognitionModel(speechRecognizerParameters.ModelPath, speechRecognitionJob);
        loadSpeechRecognitionModelObservable.Subscribe(_ =>
        {
            speechRecognitionJob.SetStatus(EJobStatus.Running);

            SpeechRecognitionUtils.DoSpeechRecognitionAsObservable(
                    songMeta,
                    audioClip,
                    minBeat,
                    lengthInBeats,
                    speechRecognizerParameters,
                    cancellationTokenSource.Token,
                    onProgress)
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
                    SpeechRecognitionUtils.MapSpeechRecognitionResultTextToNotes(songMeta, voskResultJson.result, selectedNotes, minBeat);
                    if (notify)
                    {
                        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
                    }
                });
        });
    }

    public void CreateNotesFromSpeechRecognition(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInBeats,
        bool notify)
    {
        AudioClip audioClip = GetAudioClip(speechRecognitionSampleSource);
        if (audioClip == null
            || lengthInBeats <= 0)
        {
            return;
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

        SpeechRecognitionUtils.CreateNotesFromSpeechRecognition(
                songMeta,
                audioClip,
                startBeat,
                lengthInBeats,
                CreateSpeechRecognizerParameters(),
                settings.SongEditorSettings.MidiNoteForSpeechRecognition)
            .Subscribe(createdNotes =>
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
    }

    private SpeechRecognitionParameters CreateSpeechRecognizerParameters()
    {
        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
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
