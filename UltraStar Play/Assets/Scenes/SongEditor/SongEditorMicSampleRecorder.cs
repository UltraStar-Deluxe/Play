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

public class SongEditorMicSampleRecorder : MonoBehaviour, INeedInjection, IInjectionFinishedListener
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
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.overviewAreaRecordedAudioWaveform)]
    private VisualElement overviewAreaRecordedAudioWaveform;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private MicSampleRecorder micSampleRecorder;
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

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

    private readonly Subject<bool> recordedSamplesChangedEventStream = new Subject<bool>();
    public IObservable<bool> RecordedSamplesChangedEventStream => recordedSamplesChangedEventStream;

    private int SampleRate => micSampleRecorder.FinalSampleRate.Value;

    public void OnInjectionFinished()
    {
        InitMicSampleRecorder();
        micSampleRecorder.RecordingEventStream.Subscribe(OnRecordingEvent);

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
        micSampleRecorder.IsRecording.Subscribe(newValue =>
        {
            if (newValue)
            {
                UpdateRecordingStartIndex();
            }
        });

        RecordedSamplesChangedEventStream.Buffer(new TimeSpan(0, 0, 0, 0, 800))
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
            || !settings.SongEditorSettings.IsRecordingEnabled)
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
        InitAudioClipIfNeeded();
        if (audioClip == null)
        {
            return;
        }

        audioClip.SetData(RecordingBuffer, 0);
    }

    private void OnRecordingEvent(RecordingEvent recordingEvent)
    {
        RecordSamples(recordingEvent);
    }

    private void DrawRecordedSamplesWaveForm()
    {
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
        int micDelayInSamples = (int)(micSampleRecorder.MicProfile.DelayInMillis / 1000.0 * SampleRate);
        
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
        Debug.Log($"Created AudioClip to buffer samples: {songAudioPlayer.DurationOfSongInSeconds} seconds @ {SampleRate} Hz => {audioClip.samples} samples");
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
            Debug.Log($"Reusing existing recording buffer: {songAudioPlayer.DurationOfSongInSeconds} seconds @ {SampleRate} Hz => {RecordingBuffer.Length} samples");
            return;
        }

        RecordingBuffer = new float[requiredRecordingBufferLength];
        songMetaToRecordedAudioSamples[songMeta] = RecordingBuffer;
        Debug.Log($"Initialized new recording buffer: {songAudioPlayer.DurationOfSongInSeconds} seconds @ {SampleRate} Hz => {RecordingBuffer.Length} samples");
    }

    private int GetRequiredRecordingBufferLengthInSamples()
    {
        return (int)(songAudioPlayer.DurationOfSongInMillis / 1000.0 * SampleRate);
    }
    
    private void InitMicSampleRecorder()
    {
        UpdateMicProfileAndStartOrStopRecording();

        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(_ => UpdateMicProfileAndStartOrStopRecording())
            .AddTo(gameObject);
        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.MicDelayInMillis)
            .Subscribe(_ => UpdateMicProfileAndStartOrStopRecording())
            .AddTo(gameObject);
        
        settings.SongEditorSettings
            .ObserveEveryValueChanged(it => it.IsRecordingEnabled)
            .Subscribe(newValue => StartOrStopRecording())
            .AddTo(gameObject);
        songAudioPlayer
            .ObserveEveryValueChanged(it => it.IsPlaying)
            .Subscribe(_ => StartOrStopRecording())
            .AddTo(gameObject);
    }

    private void UpdateMicProfileAndStartOrStopRecording()
    {
        micSampleRecorder.MicProfile = CreateSongEditorSpecificMicProfile();
        StartOrStopRecording();
    }

    private MicProfile CreateSongEditorSpecificMicProfile()
    {
        if (settings.SongEditorSettings.MicProfile == null)
        {
            return null;
        }

        // Copy mic profile with song editor specific sample rate.
        MicProfile micProfile = new MicProfile(settings.SongEditorSettings.MicProfile);
        micProfile.DelayInMillis = settings.SongEditorSettings.MicDelayInMillis;
        return micProfile;
    }
    
    private void StartOrStopRecording()
    {
        bool shouldBeRecoding = settings.SongEditorSettings.IsRecordingEnabled 
                                && songAudioPlayer.IsPlaying
                                && settings.SongEditorSettings.MicProfile != null
                                && settings.SongEditorSettings.MicProfile.IsEnabledAndConnected(serverSideConnectRequestManager);

        if (!shouldBeRecoding && micSampleRecorder.IsRecording.Value)
        {
            micSampleRecorder.StopRecording();
        }
        else if (shouldBeRecoding && !micSampleRecorder.IsRecording.Value)
        {
            micSampleRecorder.StartRecording();
        }
    }
}
