using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using Vosk;

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
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.overviewAreaRecordedAudioWaveform)]
    private VisualElement overviewAreaRecordedAudioWaveform;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;
    
    [Inject]
    private PitchDetectionAction pitchDetectionAction;
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private MicSampleRecorder micSampleRecorder;
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;
    
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
    private int recordingIndex;
    private int recordingStartIndex;

    private readonly Subject<bool> recordedSamplesChangedEventStream = new Subject<bool>();
    public IObservable<bool> RecordedSamplesChangedEventStream => recordedSamplesChangedEventStream;

    private int SampleRate => micSampleRecorder.FinalSampleRate.Value;

    private bool areLastNonAnalyzedSamplesAboveThreshold;
    private int analyzeStartIndex;

    private bool speechRecognizerDirty;
    private SpeechRecognitionParameters speechRecognitionParameters;
    private VoskRecognizer speechRecognizer;
    
    public void OnInjectionFinished()
    {
        InitMicSampleRecorder();
        micSampleRecorder.RecordingEventStream.Subscribe(RecordSamples);

        songAudioPlayer.PlaybackStartedEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
        });
        songAudioPlayer.JumpForwardInSongEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
        });
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
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
        
        RecordedSamplesChangedEventStream.Buffer(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(events =>
            {
                if (events.Count > 0)
                {
                    DoSpeechRecognitionForNewlyRecordedSamples();
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

    private void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            ClearRecordingBuffer();
        }
    }

    private void ClearRecordingBuffer()
    {
        Array.Clear(RecordingBuffer, 0, RecordingBuffer.Length);
        recordingIndex = 0;
        recordingStartIndex = 0;
        recordedSamplesChangedEventStream.OnNext(true);
    }

    private void DoSpeechRecognitionForNewlyRecordedSamples()
    {
        if (!settings.SongEditorSettings.SpeechRecognitionWhenRecording
            || !nonPersistentSettings.IsSongEditorRecordingEnabled.Value
            || !HasRecordedAudio
            || SpeechRecognitionUtils.IsSpeechRecognitionRunning)
        {
            return;
        }

        if (speechRecognizerDirty)
        {
            speechRecognizerDirty = false;
            InitSpeechRecognizer();
        }

        int micDelayInSamples = GetMicDelayInSamples();
        int fromIndex = analyzeStartIndex - micDelayInSamples;
        int toIndex = recordingIndex - micDelayInSamples - 1;
        int lengthInSamples = toIndex - fromIndex;
        analyzeStartIndex = recordingIndex;

        int recordingStartIndexConsideringMicDelay = recordingStartIndex - micDelayInSamples;
        double offsetInMillis = ((double)recordingStartIndexConsideringMicDelay / SampleRate) * 1000.0;
        int offsetInBeats = (int)BpmUtils.MillisecondInSongToBeat(songMeta, offsetInMillis);
        
        Debug.Log($"Analyzing speech from second {(double)fromIndex / SampleRate} to second {(double)toIndex / SampleRate} (length: {(lengthInSamples) / SampleRate} seconds)");
        speechRecognitionAction.CreateNotesFromSpeechRecognition(RecordingBuffer, fromIndex, toIndex, SampleRate, 2, true, speechRecognitionParameters, speechRecognizer, true, offsetInBeats);
    }

    private void UpdateRecordingStartIndex()
    {
        recordingStartIndex = (int)Math.Floor(songAudioPlayer.PositionInSongInSeconds * SampleRate);
        recordingIndex = recordingStartIndex;
        analyzeStartIndex = recordingIndex;
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
        int micDelayInSamples = GetMicDelayInSamples();
        
        // bool isAboveNoiseSuppressionThreshold = AbstractAudioSamplesAnalyzer.IsAboveNoiseSuppressionThreshold(
        //     recordingEvent.MicSamples,
        //     recordingEvent.NewSamplesStartIndex,
        //     recordingEvent.NewSamplesEndIndex,
        //     settings.SongEditorSettings.RecordSamplesThresholdVolumePercent);
        
        for (int i = 0; i < recordingEvent.NewSampleCount; i++)
        {
            int sampleIndexInRecordingBuffer = recordingIndex + i - micDelayInSamples;
            if (sampleIndexInRecordingBuffer > 0
                && sampleIndexInRecordingBuffer < RecordingBuffer.Length)
            {
                float recordedSampleValue = recordingEvent.MicSamples[recordingEvent.NewSamplesStartIndex + i];
                
                RecordingBuffer[sampleIndexInRecordingBuffer] = recordedSampleValue;
                
                areLastNonAnalyzedSamplesAboveThreshold = areLastNonAnalyzedSamplesAboveThreshold
                                                          || recordedSampleValue > 0.1f;
            }
        }
        recordingIndex += recordingEvent.NewSampleCount;

        HasRecordedAudio = true;
        recordedSamplesChangedEventStream.OnNext(true);
    }

    private int GetMicDelayInSamples()
    {
        return (int)(micSampleRecorder.MicProfile.DelayInMillis / 1000.0 * SampleRate);
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
        
        nonPersistentSettings
            .ObserveEveryValueChanged(it => it.IsSongEditorRecordingEnabled)
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
        bool shouldBeRecoding = nonPersistentSettings.IsSongEditorRecordingEnabled.Value 
                                && songAudioPlayer.IsPlaying
                                && settings.SongEditorSettings.MicProfile != null
                                && settings.SongEditorSettings.MicProfile.IsEnabledAndConnected(serverSideConnectRequestManager);

        if (!shouldBeRecoding && micSampleRecorder.IsRecording.Value)
        {
            micSampleRecorder.StopRecording();
        }
        else if (shouldBeRecoding && !micSampleRecorder.IsRecording.Value)
        {
            speechRecognizerDirty = true;
            micSampleRecorder.StartRecording();
        }
    }

    private void InitSpeechRecognizer()
    {
        speechRecognitionParameters = speechRecognitionAction.CreateSpeechRecognizerParameters(ESongEditorSamplesSource.Recording);
        speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);
    }
}
