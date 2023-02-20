using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorSampleRecorderControl : INeedInjection, IInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        songMetaToRecordedAudioSamples = new();
    }
    private static Dictionary<SongMeta, float[]> songMetaToRecordedAudioSamples = new();

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongEditorMicPitchTracker songEditorMicPitchTracker;

    [Inject]
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.overviewAreaRecordedAudioWaveform)]
    private VisualElement overviewAreaRecordedAudioWaveform;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;

    [Inject]
    private SongEditorNoteRecorder songEditorNoteRecorder;

    private AudioWaveFormVisualization recordedAudioWaveFormVisualization;

    private AudioClip audioClip;
    public AudioClip AudioClip
    {
        get
        {
            InitAudioClipIfNeeded();
            return audioClip;
        }
    }

    public bool HasRecordedAudio { get; private set; }

    public float[] RecordingBuffer { get; private set; }
    private int recordingStartIndex;
    private int speechRecognitionStartBeat;

    private Subject<bool> recordedSamplesChangedEventStream = new Subject<bool>();
    public IObservable<bool> RecordedSamplesChangedEventStream => recordedSamplesChangedEventStream;

    private int SampleRate => songEditorMicPitchTracker.MicSampleRecorder.FinalSampleRate.Value;

    public void OnInjectionFinished()
    {
        songEditorMicPitchTracker.MicSampleRecorder.RecordingEventStream.Subscribe(OnRecordingEvent);
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
            UpdateSpeechRecognitionStartBeat();
        });
        songAudioPlayer.JumpForwardInSongEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
            UpdateSpeechRecognitionStartBeat();
        });
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
            UpdateSpeechRecognitionStartBeat();
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(evt =>
        {
            FillAudioClipWithRecordingBuffer();
            DoSpeechRecognitionForNewlyRecordedSamples();
        });
        songEditorMicPitchTracker.MicSampleRecorder.IsRecording.Subscribe(newValue =>
        {
            if (newValue)
            {
                UpdateRecordingStartIndex();
            }
        });

        recordedSamplesChangedEventStream.Buffer(new TimeSpan(0, 0, 0, 0, 800))
            .Subscribe(events =>
            {
                if (events.Count > 0)
                {
                    DrawRecordedSamplesWaveForm();
                }
            });

        // Load recorded samples from cache
        if (songMetaToRecordedAudioSamples.ContainsKey(songMeta))
        {
            InitRecordingBufferIfNeeded();
            if (float.IsNaN(overviewAreaRecordedAudioWaveform.contentRect.width)
                || float.IsNaN(overviewAreaRecordedAudioWaveform.contentRect.height))
            {
                overviewAreaRecordedAudioWaveform.RegisterCallback<GeometryChangedEvent>(evt => DrawRecordedSamplesWaveForm());
            }
            else
            {
                DrawRecordedSamplesWaveForm();
            }
        }
    }

    private void DoSpeechRecognitionForNewlyRecordedSamples()
    {
        if (!settings.SongEditorSettings.DetectSpeechAfterRecording
            || !songEditorNoteRecorder.IsRecordingEnabled)
        {
            return;
        }

        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);
        int lengthInBeats = currentBeat - speechRecognitionStartBeat;
        Debug.Log($"Analyzing speech from beat {speechRecognitionStartBeat} to beat {currentBeat} (length: {lengthInBeats} beats)");
        speechRecognitionAction.CreateNotesFromSpeechRecognition(speechRecognitionStartBeat, lengthInBeats, ESongEditorSamplesSource.Recording, 2, true);

        UpdateSpeechRecognitionStartBeat();
    }

    private void UpdateRecordingStartIndex()
    {
        recordingStartIndex = (int)Math.Floor(songAudioPlayer.PositionInSongInSeconds * SampleRate);
    }

    private void UpdateSpeechRecognitionStartBeat()
    {
        speechRecognitionStartBeat = (int)songAudioPlayer.GetCurrentBeat(true);
    }

    private void FillAudioClipWithRecordingBuffer()
    {
        if (!songEditorNoteRecorder.IsRecordingEnabled)
        {
            return;
        }

        InitAudioClipIfNeeded();
        if (audioClip == null)
        {
            return;
        }

        audioClip.SetData(RecordingBuffer, 0);
    }

    private void OnRecordingEvent(RecordingEvent recordingEvent)
    {
        if (settings.SongEditorSettings.RecordSamplesInsteadOfNotes)
        {
            RecordSamples(recordingEvent);
        }
    }

    private void DrawRecordedSamplesWaveForm()
    {
        if (!settings.SongEditorSettings.RecordSamplesInsteadOfNotes)
        {
            return;
        }

        if (recordedAudioWaveFormVisualization == null)
        {
            recordedAudioWaveFormVisualization = new AudioWaveFormVisualization(gameObject, overviewAreaRecordedAudioWaveform)
            {
                WaveformColor = Colors.red,
            };
        }

        recordedAudioWaveFormVisualization.DrawWaveFormMinAndMaxValues(RecordingBuffer);
    }

    private void RecordSamples(RecordingEvent recordingEvent)
    {
        InitRecordingBufferIfNeeded();

        // Copy samples from mic buffer to recording buffer
        int micDelayInSamples = songEditorMicPitchTracker.MicSampleRecorder.MicProfile.DelayInMillis / 1000 * SampleRate;
        // bool isAboveNoiseSuppressionThreshold = AbstractAudioSamplesAnalyzer.IsAboveNoiseSuppressionThreshold(
        //     recordingEvent.MicSamples,
        //     recordingEvent.NewSamplesStartIndex,
        //     recordingEvent.NewSamplesEndIndex,
        //     settings.SongEditorSettings.RecordSamplesThresholdVolumePercent);
        for (int i = 0; i < recordingEvent.NewSampleCount; i++)
        {
            int sampleIndexInRecordingBuffer = recordingStartIndex + i - micDelayInSamples;
            if (sampleIndexInRecordingBuffer > 0
                && sampleIndexInRecordingBuffer < RecordingBuffer.Length)
            {
                float recordedSampleValue = recordingEvent.MicSamples[recordingEvent.NewSamplesStartIndex + i];
                RecordingBuffer[sampleIndexInRecordingBuffer] = recordedSampleValue;
                // RecordingBuffer[sampleIndexInRecordingBuffer] = isAboveNoiseSuppressionThreshold
                //     ? recordedSampleValue
                //     : 0;
            }
        }
        recordingStartIndex += recordingEvent.NewSampleCount;

        HasRecordedAudio = true;
        recordedSamplesChangedEventStream.OnNext(true);
    }

    private void InitAudioClipIfNeeded()
    {
        if (audioClip != null
            && (audioClip.frequency != SampleRate
                || audioClip.samples != GetRequiredRecordingBufferLengthInSamples()))
        {
            // Create new recording buffer with different settings
            GameObject.Destroy(audioClip);
            audioClip = null;
        }

        if (audioClip != null)
        {
            return;
        }

        InitRecordingBufferIfNeeded();
        if (RecordingBuffer.IsNullOrEmpty())
        {
            return;
        }

        int channels = 1;
        audioClip = AudioClip.Create(GetType().Name, RecordingBuffer.Length, channels, SampleRate, false);
        audioClip.SetData(RecordingBuffer, 0);
    }

    private void InitRecordingBufferIfNeeded()
    {
        if (!RecordingBuffer.IsNullOrEmpty())
        {
            return;
        }

        int requiredRecordingBufferLength = GetRequiredRecordingBufferLengthInSamples();
        if (songMetaToRecordedAudioSamples.TryGetValue(songMeta, out float[] cachedRecordingBuffer)
            && (requiredRecordingBufferLength <= 0
                || requiredRecordingBufferLength == cachedRecordingBuffer.Length))
        {
            RecordingBuffer = cachedRecordingBuffer;
            HasRecordedAudio = cachedRecordingBuffer.AnyMatch(sample => sample != 0);
            return;
        }

        RecordingBuffer = new float[requiredRecordingBufferLength];
        songMetaToRecordedAudioSamples[songMeta] = RecordingBuffer;
    }

    private int GetRequiredRecordingBufferLengthInSamples()
    {
        return (int)(songAudioPlayer.DurationOfSongInMillis / 1000.0 * SampleRate);
    }
}
