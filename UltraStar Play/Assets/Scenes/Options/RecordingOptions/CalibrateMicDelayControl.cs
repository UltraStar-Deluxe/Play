using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CalibrateMicDelayControl : MonoBehaviour, INeedInjection
{
    // The notes that are played for calibration.
    [InjectedInInspector]
    public List<MidiNoteAndFrequency> midiNoteNameAndFrequencies;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildrenIncludeInactive)]
    private SineToneAudioSource sineToneAudioSource;

    [Inject]
    private NewestSamplesMicPitchTracker micPitchTracker;

    [Inject]
    private RecordingOptionsSceneControl recordingOptionsSceneControl;

    [Inject]
    private BackgroundMusicManager backgroundMusicManager;

    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    private enum ECalibrationPhase
    {
        None,
        Volume,
        MicDelay,
    }
    private ECalibrationPhase calibrationPhase = ECalibrationPhase.None;

    private readonly Subject<CalibrationResult> calibrationResultEventStream = new();
    public IObservable<CalibrationResult> CalibrationResultEventStream => calibrationResultEventStream;

    private float oldBackgroundMusicVolume = -1;

    private MicProfile MicProfile => micPitchTracker.MicProfile;
    private float[] MicSamples => micPitchTracker.MicSamples;

    void Start()
    {
        // Sanity check
        if (midiNoteNameAndFrequencies.IsNullOrEmpty())
        {
            throw new UnityException("No notes configured for calibration");
        }
        sineToneAudioSource.gameObject.SetActive(false);

        settings.ObserveEveryValueChanged(it => it.PreferPortAudio)
            .Subscribe(_ => sineToneAudioSource.UsePortAudio = settings.PreferPortAudio)
            .AddTo(gameObject);

        // Stop sine tone after calibration
        calibrationResultEventStream
            .ObserveOnMainThread()
            .Subscribe(_ =>
            {
                DeactivateSineTone();
                calibrationPhase = ECalibrationPhase.None;
            });
    }

    public async void StartCalibration()
    {
        if (calibrationPhase is not ECalibrationPhase.None
            || MicProfile == null)
        {
            return;
        }

        if (oldBackgroundMusicVolume < 0)
        {
            oldBackgroundMusicVolume = backgroundMusicManager.BackgroundMusicAudioSource.volume;
        }
        backgroundMusicManager.BackgroundMusicAudioSource.volume = 0;
        sineToneAudioSource.gameObject.SetActive(true);
        sineToneAudioSource.SkipAudioOutput = true;
        sineToneAudioSource.Play();

        await Task.Run(CalibrateAsync);
    }

    private async Task CalibrateAsync()
    {
        CalibrationResult calibrationResult;
        IDisposable beatPitchEventsStreamSubscription = null;
        BeatPitchEventsDto lastReceivedBeatPitchEventsDto = null;
        try
        {
            if (MicProfile.IsInputFromConnectedClient)
            {
                if (!serverSideCompanionClientManager.TryGet(MicProfile.ConnectedClientId, out ICompanionClientHandler companionClientHandler))
                {
                    throw new Exception("Mic calibration aborted, no connected client found for mic input.");
                }

                beatPitchEventsStreamSubscription = companionClientHandler.ReceivedMessageStream
                    .Where(message => message is BeatPitchEventsDto)
                    .Select(message => message as BeatPitchEventsDto)
                    .Subscribe(dto => lastReceivedBeatPitchEventsDto = dto);
            }

            VolumeCalibrationResult volumeCalibrationResult = await VolumeCalibrationAsync();
            float volumeThreshold = volumeCalibrationResult.MaxRecordedVolume / 2;

            MicDelayCalibrationResult micDelayCalibrationResult = await MicDelayCalibrationAsync(
                volumeThreshold,
                () => lastReceivedBeatPitchEventsDto);
            calibrationResult = new CalibrationResult(micDelayCalibrationResult.DelaysInMillis);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Calibration failed");
            calibrationResultEventStream.OnNext(new CalibrationResult());
            return;
        }
        finally
        {
            beatPitchEventsStreamSubscription?.Dispose();
        }

        calibrationResultEventStream.OnNext(calibrationResult);
    }

    private async Task<VolumeCalibrationResult> VolumeCalibrationAsync()
    {
        Debug.Log($"Starting volume calibration of '{MicProfile.GetDisplayNameWithChannel()}' on thread {Thread.CurrentThread.ManagedThreadId}");

        if (MicProfile.IsInputFromConnectedClient)
        {
            Debug.Log("Returning volume threshold 0 for mic input from connected client");
            return new VolumeCalibrationResult(0);
        }

        calibrationPhase = ECalibrationPhase.Volume;
        float maxRecordedVolume = 0;
        PlayNextSineTone(GetIterationFrequency(0));

        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        if (calibrationPhase is not ECalibrationPhase.Volume)
        {
            throw new Exception("Volume calibration aborted");
        }

        // Determine max recorded volume from newly recorded samples
        int newSampleCount = CalculateNewSampleCount(micPitchTracker.FinalSampleRate.Value, startTimeInMillis, TimeUtils.GetUnixTimeMilliseconds());
        for (int sampleIndex = MicSamples.Length - newSampleCount; sampleIndex < MicSamples.Length; sampleIndex++)
        {
            float sample = MicSamples[sampleIndex];
            maxRecordedVolume = Mathf.Max(maxRecordedVolume, Mathf.Abs(sample));
        }

        float minVolume = 0.01f;
        if (maxRecordedVolume < minVolume)
        {
            throw new Exception($"Volume calibration failed, max recorded volume too low, should be above {minVolume} but was {maxRecordedVolume.ToStringInvariantCulture()}");
        }

        Debug.Log($"Volume calibration successful, max recorded volume: {maxRecordedVolume}");
        return new VolumeCalibrationResult(maxRecordedVolume);
    }

    private int CalculateNewSampleCount(int sampleRate, long startTimeInMillis, long currentTimeInMillis)
    {
        long durationSinceStartInMillis = currentTimeInMillis - startTimeInMillis;
        double durationSinceStartInSeconds = durationSinceStartInMillis / 1000.0;
        int newSampleCount = (int)Math.Floor(sampleRate * durationSinceStartInSeconds);
        return newSampleCount;
    }

    private async Task<MicDelayCalibrationResult> MicDelayCalibrationAsync(
        float thresholdVolume,
        Func<BeatPitchEventsDto> lastReceivedBeatPitchEventsDtoGetter)
    {
        Debug.Log($"Starting mic delay calibration of '{MicProfile.GetDisplayNameWithChannel()}' on thread {Thread.CurrentThread.ManagedThreadId}");
        calibrationPhase = ECalibrationPhase.MicDelay;
        List<long> delaysInMillis = new();
        int iteration = 1;
        int pauseTimeInMillis = 500;

        if (!MicProfile.IsInputFromConnectedClient)
        {
            // Short mute after sine tone that was used to get the volume threshold.
            MuteSineTone();
            await Task.Delay(TimeSpan.FromMilliseconds(pauseTimeInMillis));
        }

        int sineToneFrequency = GetIterationFrequency(iteration);
        int sineToneMidiNote = MidiUtils.GetMidiNoteForFrequency(sineToneFrequency);
        PlayNextSineTone(sineToneFrequency);

        long maxIterationTimeInMillis = 1000;
        long iterationStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long previousSampleCheckTimeInMillis = iterationStartTimeInMillis;
        while (iteration < midiNoteNameAndFrequencies.Count)
        {
            if (calibrationPhase is not ECalibrationPhase.MicDelay)
            {
                throw new Exception("Mic delay calibration aborted");
            }

            long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
            if (currentTimeInMillis - iterationStartTimeInMillis > maxIterationTimeInMillis)
            {
                throw new Exception($"Calibration of '{MicProfile.GetDisplayNameWithChannel()}' timed out");
            }

            // Check if newly recorded samples are above threshold volume
            bool isAboveThresholdVolume = false;
            bool isCorrectPitchFromCompanionClient = false;
            int newSampleCount = CalculateNewSampleCount(micPitchTracker.FinalSampleRate.Value, previousSampleCheckTimeInMillis, currentTimeInMillis);
            previousSampleCheckTimeInMillis = currentTimeInMillis;
            // Debug.Log($"Mic delay calibration iteration {iteration}, new sample count: {newSampleCount}");
            if (MicProfile.IsInputFromConnectedClient)
            {
                // Wait for correct pitch to be reported from connected client
                BeatPitchEventsDto receivedBeatPitchEventsDto = lastReceivedBeatPitchEventsDtoGetter?.Invoke();
                if (receivedBeatPitchEventsDto != null)
                {
                    isCorrectPitchFromCompanionClient = receivedBeatPitchEventsDto
                        .BeatPitchEvents
                        .AnyMatch(dto => dto.MidiNote == sineToneMidiNote);
                }
            }
            else
            {
                // Wait for mic volume threshold to be reached
                for (int sampleIndex = MicSamples.Length - newSampleCount; sampleIndex < MicSamples.Length; sampleIndex++)
                {
                    float sample = MicSamples[sampleIndex];
                    if (sample >= Mathf.Abs(thresholdVolume))
                    {
                        isAboveThresholdVolume = true;
                        break;
                    }
                }
            }

            if ((!MicProfile.IsInputFromConnectedClient && isAboveThresholdVolume)
                || (MicProfile.IsInputFromConnectedClient && isCorrectPitchFromCompanionClient))
            {
                long delayInMillis = currentTimeInMillis - iterationStartTimeInMillis;
                delaysInMillis.Add(delayInMillis);
                Debug.Log($"Mic delay calibration iteration {iteration} successful, delay: {delayInMillis} ms");

                iteration++;
                if (iteration < midiNoteNameAndFrequencies.Count)
                {
                    // Start next iteration
                    MuteSineTone();
                    await Task.Delay(TimeSpan.FromMilliseconds(pauseTimeInMillis));
                    sineToneFrequency = GetIterationFrequency(iteration);
                    sineToneMidiNote = MidiUtils.GetMidiNoteForFrequency(sineToneFrequency);
                    PlayNextSineTone(sineToneFrequency);
                    iterationStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
                    previousSampleCheckTimeInMillis = iterationStartTimeInMillis;
                }
                else
                {
                    // All iterations done
                    Debug.Log($"Mic delay calibration successful, delays (ms): {delaysInMillis.JoinWith(", ")}");
                    return new MicDelayCalibrationResult(delaysInMillis);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
        }

        throw new Exception("Mic delay calibration failed");
    }

    private void MuteSineTone()
    {
        sineToneAudioSource.SkipAudioOutput = true;
    }

    private void UnmuteSineTone()
    {
        sineToneAudioSource.SkipAudioOutput = false;
    }

    private void DeactivateSineTone()
    {
        Debug.Log("Stopping sine tone");
        sineToneAudioSource.Stop();
        sineToneAudioSource.gameObject.SetActive(false);
        backgroundMusicManager.BackgroundMusicAudioSource.volume = oldBackgroundMusicVolume;
    }

    private void PlayNextSineTone(int frequency)
    {
        Debug.Log($"Playing sine tone with {frequency} Hz");
        sineToneAudioSource.Frequency = frequency;
        UnmuteSineTone();
    }

    private int GetIterationFrequency(int iteration)
    {
        if (midiNoteNameAndFrequencies.Count < iteration)
        {
            throw new IndexOutOfRangeException($"No note configured for iteration {iteration}");
        }
        return midiNoteNameAndFrequencies[iteration].frequency;
    }

    private void OnDestroy()
    {
        if (oldBackgroundMusicVolume >= 0)
        {
            backgroundMusicManager.BackgroundMusicAudioSource.volume = oldBackgroundMusicVolume;
        }
    }

    [Serializable]
    public struct MidiNoteAndFrequency
    {
        public string midiNoteName;
        public int frequency;
    }

    public class CalibrationResult
    {
        public bool IsSuccess { get; private set; }
        public List<long> DelaysInMilliseconds { get; private set; }

        public CalibrationResult()
            : this(new List<long>())
        {
        }

        public CalibrationResult(List<long> delaysInMilliseconds)
        {
            DelaysInMilliseconds = delaysInMilliseconds;
            IsSuccess = !delaysInMilliseconds.IsNullOrEmpty();
        }
    }

    private class VolumeCalibrationResult
    {
        public bool Success { get; private set; }
        public float MaxRecordedVolume { get; private set; }

        public VolumeCalibrationResult()
        {
        }

        public VolumeCalibrationResult(float maxRecordedVolume)
        {
            MaxRecordedVolume = maxRecordedVolume;
            Success = true;
        }

        public void SetResult(float maxRecordedVolume)
        {
            MaxRecordedVolume = maxRecordedVolume;
            Success = true;
        }
    }

    private class MicDelayCalibrationResult
    {
        public bool Success { get; private set; }
        public List<long> DelaysInMillis { get; private set; }

        public MicDelayCalibrationResult()
        {
            DelaysInMillis = new();
        }

        public MicDelayCalibrationResult(List<long> delaysInMillis)
        {
            DelaysInMillis = delaysInMillis;
        }

        public void SetResult(List<long> delaysInMillis)
        {
            DelaysInMillis = delaysInMillis;
            Success = true;
        }
    }
}
