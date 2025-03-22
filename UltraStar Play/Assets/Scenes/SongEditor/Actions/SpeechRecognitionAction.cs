using System;
using System.Collections.Generic;
using System.Linq;
using NHyphenator;
using UniInject;
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

    private static int speechRecognitionProcessCount;

    [Inject] private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject] private SongAudioPlayer songAudioPlayer;

    [Inject] private SpeechRecognitionManager speechRecognitionManager;

    [Inject] private SpeechRecognitionNoteCreator speechRecognitionNoteCreator;

    [Inject] private SongEditorLayerManager songEditorLayerManager;

    [Inject] private EditorNoteDisplayer editorNoteDisplayer;

    [Inject] private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject] private JobManager jobManager;

    [Inject] private SpeechRecognizerProvider speechRecognizerProvider;

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

        int audioClipFrequency = audioClip.frequency;
        int minBeat = SongMetaUtils.GetMinBeat(selectedNotes);
        int lengthInBeats = SongMetaUtils.GetLengthInBeats(selectedNotes);

        SpeechRecognizerConfig speechRecognizerConfig = CreateSpeechRecognizerParameters();

        try
        {
            SpeechRecognizer speechRecognizer = await speechRecognizerProvider.GetSpeechRecognizerJob(speechRecognizerConfig)
                .GetResultAsync();

            float[] monoAudioSamples = SongMetaAudioSampleUtils.GetMonoSamples(songMeta, audioClip, minBeat, lengthInBeats);

            await Awaitable.BackgroundThreadAsync();
            SpeechRecognitionResult speechRecognitionResult = await speechRecognitionManager.ProcessSongMetaJob(
                new SpeechRecognitionInputSamples(monoAudioSamples, 0, monoAudioSamples.Length - 1, audioClipFrequency),
                speechRecognizer)
                .GetResultAsync();

            await Awaitable.MainThreadAsync();
            SpeechRecognitionResultTextToNotesMapper.MapSpeechRecognitionResultTextToNotes(songMeta, speechRecognitionResult.Words, selectedNotes, minBeat);
            if (notify)
            {
                songMetaChangedEventStream.OnNext(new LyricsChangedEvent());
            }
        }
        catch (Exception ex)
        {
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
        SpeechRecognizerConfig speechRecognizerConfig,
        int offsetInBeats)
    {
        await CreateNotesFromSpeechRecognitionAsync(
            monoAudioSamples,
            startIndex,
            endIndex,
            sampleRate,
            spaceBetweenNotesInMillis,
            notify,
            speechRecognizerConfig,
            offsetInBeats);
    }

    public async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionAsync(
        float[] monoAudioSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognizerConfig speechRecognizerConfig,
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
            List<Note> createdNotes = await speechRecognitionNoteCreator.CreateNotesFromSpeechRecognitionJob(
                    new CreateNotesFromSpeechRecognitionConfig
                    {
                        SpeechRecognizerConfig = speechRecognizerConfig,
                        InputSamples = new SpeechRecognitionInputSamples(monoAudioSamples, startIndex, endIndex, sampleRate),
                        MidiNote = settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                        SongMeta = songMeta,
                        OffsetInBeats = offsetInBeats,
                        Hyphenator = hyphenator,
                        SpaceInMillisBetweenNotes = settings.SongEditorSettings.SpaceBetweenNotesInMillis,
                    })
                .GetResultAsync();

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
                songMetaChangedEventStream.OnNext(new NotesChangedEvent());
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
        SpeechRecognizerConfig speechRecognizerConfig)
    {
        try
        {
            await CreateNotesFromSpeechRecognitionAsync(startBeat,
                lengthInBeats,
                speechRecognitionSampleSource,
                spaceBetweenNotesInMillis,
                notify,
                speechRecognizerConfig);
        }
        catch (Exception ex)
        {
            if (ex is JobAlreadyRunningException)
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.job_error_alreadyInProgress));
            }
            else
            {
                NotificationManager.CreateNotification(Translation.Get(Translation.Get(R.Messages.job_speechRecognition_errorWithReason,
                    "reason", ex.Message)));
            }
        }
    }

    private async Awaitable<List<Note>> CreateNotesFromSpeechRecognitionAsync(
        int startBeat,
        int lengthInBeats,
        ESongEditorSamplesSource speechRecognitionSampleSource,
        int spaceBetweenNotesInMillis,
        bool notify,
        SpeechRecognizerConfig speechRecognizerConfig)
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

        float[] monoAudioSamples = SongMetaAudioSampleUtils.GetMonoSamples(songMeta, audioClip, startBeat, lengthInBeats);

        Hyphenator hyphenator = settings.SongEditorSettings.SplitSyllablesAfterSpeechRecognition
            ? SettingsUtils.CreateHyphenator(settings)
            : null;

        List<Note> createdNotes = await speechRecognitionNoteCreator.CreateNotesFromSpeechRecognitionJob(
                new CreateNotesFromSpeechRecognitionConfig
                {
                    SpeechRecognizerConfig = speechRecognizerConfig,
                    InputSamples = new SpeechRecognitionInputSamples(monoAudioSamples, 0, monoAudioSamples.Length - 1, audioClip.frequency),
                    MidiNote = settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                    SongMeta = songMeta,
                    OffsetInBeats = startBeat,
                    Hyphenator = hyphenator,
                    SpaceInMillisBetweenNotes = settings.SongEditorSettings.SpaceBetweenNotesInMillis
                })
            .GetResultAsync();

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
            songMetaChangedEventStream.OnNext(new NotesChangedEvent());
        }

        return createdNotes;
    }

    public SpeechRecognizerConfig CreateSpeechRecognizerParameters()
    {
        return new SpeechRecognizerConfig(
            SettingsUtils.GetSpeechRecognitionModelPath(settings),
            SettingsUtils.GetSpeechRecognitionLanguage(settings),
            settings.SongEditorSettings.SpeechRecognitionPrompt);
    }
}
