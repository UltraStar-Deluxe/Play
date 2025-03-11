using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMicSampleRecorder : MonoBehaviour, INeedInjection
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
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.overviewAreaRecordedAudioWaveform)]
    private VisualElement overviewAreaRecordedAudioWaveform;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

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

    private readonly Subject<VoidEvent> recordedSamplesChangedEventStream = new();
    public IObservable<VoidEvent> RecordedSamplesChangedEventStream => recordedSamplesChangedEventStream;

    private bool areLastNonAnalyzedSamplesAboveThreshold;
    private int analyzeStartIndex;

    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get => micProfile;
        set
        {
            DisposeMicSampleRecorderDisposables();

            micProfile = value;

            if (MicSampleRecorder == null)
            {
                return;
            }

            // Listen to changes
            micSampleRecorderDisposables.Add(MicSampleRecorder.FinalSampleRate.Subscribe(newValue => FinalSampleRate.Value = newValue));
            micSampleRecorderDisposables.Add(MicSampleRecorder.IsRecording.Subscribe(newValue => IsRecording.Value = newValue));
            micSampleRecorderDisposables.Add(MicSampleRecorder.RecordingEventStream.Subscribe(evt => OnRecordingEvent(evt)));
        }
    }

    public float[] MicSamples
    {
        get
        {
            if (MicSampleRecorder == null)
            {
                return Array.Empty<float>();
            }

            return MicSampleRecorder.MicSamples;
        }
    }

    public ReactiveProperty<int> FinalSampleRate { get; private set; } = new(MicSampleRecorder.DefaultSampleRate);
    public ReactiveProperty<bool> IsRecording { get; private set; } = new();

    private MicSampleRecorder MicSampleRecorder => micSampleRecorderManager.GetOrCreateMicSampleRecorder(micProfile);

    private readonly List<IDisposable> micSampleRecorderDisposables = new();

    public void Start()
    {
        InitMicSampleRecorder();

        songAudioPlayer.PlaybackStartedEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
        });
        songAudioPlayer.JumpForwardEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
        });
        songAudioPlayer.JumpBackEventStream.Subscribe(evt =>
        {
            UpdateRecordingStartIndex();
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(evt =>
        {
            FillAudioClipWithRecordingBuffer();
            DoSpeechRecognitionForNewlyRecordedSamples();
        });
        IsRecording.Subscribe(newValue =>
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

        // RecordedSamplesChangedEventStream.Buffer(new TimeSpan(0, 0, 0, 0, 1000))
        //     .Subscribe(events =>
        //     {
        //         if (events.Count > 0)
        //         {
        //             DoSpeechRecognitionForNewlyRecordedSamples();
        //         }
        //     });

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
        recordedSamplesChangedEventStream.OnNext(VoidEvent.instance);
    }

    private void DoSpeechRecognitionForNewlyRecordedSamples()
    {
        if (!settings.SongEditorSettings.SpeechRecognitionWhenRecording
            || !nonPersistentSettings.IsSongEditorRecordingEnabled.Value
            || !HasRecordedAudio
            || speechRecognitionManager.IsSpeechRecognitionRunning)
        {
            return;
        }

        int sampleRate = FinalSampleRate.Value;

        int micDelayInSamples = GetMicDelayInSamples();
        int fromIndex = analyzeStartIndex - micDelayInSamples;
        int toIndex = recordingIndex - micDelayInSamples - 1;
        int lengthInSamples = toIndex - fromIndex;
        analyzeStartIndex = recordingIndex;

        double gapShiftInBeats = SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, songMeta.GapInMillis);

        Debug.Log($"Analyzing speech of newly recorded samples from second {(double)fromIndex / sampleRate} to second {(double)toIndex / sampleRate} (length: {(lengthInSamples) / sampleRate} seconds)");
        speechRecognitionAction.CreateNotesFromSpeechRecognition(
            RecordingBuffer,
            fromIndex,
            toIndex,
            sampleRate,
            150,
            true,
            speechRecognitionAction.CreateSpeechRecognizerParameters(),
            -(int)gapShiftInBeats);
    }

    private void UpdateRecordingStartIndex()
    {
        recordingStartIndex = (int)Math.Floor(songAudioPlayer.PositionInSeconds * FinalSampleRate.Value);
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
            int textureWidth = 512;
            int textureHeight = 128;
            recordedAudioWaveFormVisualization = new AudioWaveFormVisualization(
                gameObject,
                overviewAreaRecordedAudioWaveform,
                textureWidth,
                textureHeight,
                "song editor recorded audio visualization");
            recordedAudioWaveFormVisualization.WaveformColor = Colors.red;
        }

        recordedAudioWaveFormVisualization.DrawAudioWaveForm(RecordingBuffer);
    }

    private void OnRecordingEvent(RecordingEvent recordingEvent)
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
        recordedSamplesChangedEventStream.OnNext(VoidEvent.instance);
    }

    private int GetMicDelayInSamples()
    {
        if (MicSampleRecorder == null)
        {
            return 0;
        }

        return (int)(MicSampleRecorder.MicProfile.DelayInMillis / 1000.0 * FinalSampleRate.Value);
    }

    private void InitAudioClipIfNeeded()
    {
        if (audioClip != null
            && (audioClip.frequency != FinalSampleRate.Value
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
        audioClip = AudioClip.Create(GetType().Name, RecordingBuffer.Length, channels, FinalSampleRate.Value, false);
        audioClip.SetData(RecordingBuffer, 0);
        Debug.Log($"Created AudioClip to buffer samples: {songAudioPlayer.DurationInSeconds} seconds @ {FinalSampleRate.Value} Hz => {audioClip.samples} samples");
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
            Debug.Log($"Reusing existing recording buffer: {songAudioPlayer.DurationInSeconds} seconds @ {FinalSampleRate.Value} Hz => {RecordingBuffer.Length} samples");
            return;
        }

        RecordingBuffer = new float[requiredRecordingBufferLength];
        songMetaToRecordedAudioSamples[songMeta] = RecordingBuffer;
        Debug.Log($"Initialized new recording buffer: {songAudioPlayer.DurationInSeconds} seconds @ {FinalSampleRate.Value} Hz => {RecordingBuffer.Length} samples");
    }

    private int GetRequiredRecordingBufferLengthInSamples()
    {
        return (int)(songAudioPlayer.DurationInMillis / 1000.0 * FinalSampleRate.Value);
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
        MicProfile = CreateSongEditorSpecificMicProfile();
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
        if (MicSampleRecorder == null)
        {
            return;
        }

        bool shouldBeRecoding = nonPersistentSettings.IsSongEditorRecordingEnabled.Value
                                && songAudioPlayer.IsPlaying
                                && settings.SongEditorSettings.MicProfile != null
                                && settings.SongEditorSettings.MicProfile.IsEnabledAndConnected(serverSideCompanionClientManager);

        if (!shouldBeRecoding && MicSampleRecorder.IsRecording.Value)
        {
            MicSampleRecorder.StopRecording();
        }
        else if (shouldBeRecoding && !MicSampleRecorder.IsRecording.Value)
        {
            MicSampleRecorder.StartRecording();
        }
    }

    private void OnDestroy()
    {
        DisposeMicSampleRecorderDisposables();
    }

    private void DisposeMicSampleRecorderDisposables()
    {
        micSampleRecorderDisposables.ForEach(it => it.Dispose());
        micSampleRecorderDisposables.Clear();
    }
}
