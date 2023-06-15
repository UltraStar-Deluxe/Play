using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CalibrateMicDelayControl : MonoBehaviour, INeedInjection
{
    private const long TimeoutInMillis = 2000;
    private const long PauseTimeInMillis = 500;
    
    // The notes that are played for calibration.
    [InjectedInInspector]
    public List<MidiNoteAndFrequency> midiNoteNameAndFrequencies;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildrenIncludeInactive)]
    private SineToneAudioGenerator sineToneAudioGenerator;

    [Inject]
    private NewestSamplesMicPitchTracker micPitchTracker;

    [Inject]
    private RecordingOptionsSceneControl recordingOptionsSceneControl;

    [Inject]
    private BackgroundMusicManager backgroundMusicManager;

    public MicProfile MicProfile { get; set; }

    private readonly Subject<CalibrationResult> calibrationResultEventStream = new();
    public IObservable<CalibrationResult> CalibrationResultEventStream => calibrationResultEventStream;

    private bool isCalibrationInProgress;

    private long currentIterationStartTimeInMillis;
    private long nextIterationStartTimeInMillis;

    private List<long> delaysInMillis = new();
    private int currentIteration;
    private float oldBackgroundMusicVolume = -1;

    void Start()
    {
        // Sanity check
        if (midiNoteNameAndFrequencies.IsNullOrEmpty())
        {
            throw new UnityException("No notes configured for calibration");
        }
        sineToneAudioGenerator.gameObject.SetActive(false);
        
        micPitchTracker.PitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
        recordingOptionsSceneControl.ConnectedClientBeatPitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
    }

    void Update()
    {
        if (!isCalibrationInProgress)
        {
            return;
        }

        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        if (nextIterationStartTimeInMillis > 0
            && currentTimeInMillis >= nextIterationStartTimeInMillis)
        {
            nextIterationStartTimeInMillis = 0;
            StartIteration(currentIteration);
        }
        else if (currentTimeInMillis - currentIterationStartTimeInMillis > TimeoutInMillis)
        {
            OnCalibrationTimedOut();
        }
    }

    public void StartCalibration()
    {
        if (isCalibrationInProgress)
        {
            return;
        }
        isCalibrationInProgress = true;
        
        Debug.Log("Starting mic delay calibration");
        delaysInMillis.Clear();
        currentIteration = 0;
        oldBackgroundMusicVolume = backgroundMusicManager.BackgroundMusicAudioSource.volume;
        backgroundMusicManager.BackgroundMusicAudioSource.volume = 0;
        sineToneAudioGenerator.gameObject.SetActive(true);
        sineToneAudioGenerator.Play();
        StartIteration(currentIteration);
    }

    private void StopCalibration()
    {
        Debug.Log("Stopping mic delay calibration");
        sineToneAudioGenerator.Stop();
        sineToneAudioGenerator.gameObject.SetActive(false);
        backgroundMusicManager.BackgroundMusicAudioSource.volume = oldBackgroundMusicVolume;
        isCalibrationInProgress = false;
    }
    
    private void OnCalibrationTimedOut()
    {
        Debug.Log($"Mic delay calibration iteration {currentIteration} timed out");
        StopCalibration();
        calibrationResultEventStream.OnNext(new CalibrationResult
        {
            IsSuccess = false
        });
    }

    public void OnEndCalibration()
    {
        Debug.Log($"Mic delay calibration successful: median delay of {delaysInMillis.Count} values: {delaysInMillis[delaysInMillis.Count/2]}");
        StopCalibration();
        calibrationResultEventStream.OnNext(new CalibrationResult
        {
            IsSuccess = true,
            DelaysInMilliseconds = new List<long>(delaysInMillis)
        });
    }

    private void StartIteration(int iteration)
    {
        Debug.Log($"Starting mic delay calibration iteration {iteration}");
        currentIterationStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        sineToneAudioGenerator.Frequency = GetFrequency(iteration);
        sineToneAudioGenerator.Play();
    }

    private string GetMidiNoteName(int iteration)
    {
        if (midiNoteNameAndFrequencies.Count < iteration)
        {
            throw new IndexOutOfRangeException($"No note configured for iteration {iteration}");
        }
        return midiNoteNameAndFrequencies[iteration].midiNoteName;
    }
    
    private int GetFrequency(int iteration)
    {
        if (midiNoteNameAndFrequencies.Count < iteration)
        {
            throw new IndexOutOfRangeException($"No note configured for iteration {iteration}");
        }
        return midiNoteNameAndFrequencies[iteration].frequency;
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (pitchEvent == null || !isCalibrationInProgress || nextIterationStartTimeInMillis > 0)
        {
            return;
        }

        string targetMidiNoteName = GetMidiNoteName(currentIteration);
        if (MidiUtils.GetAbsoluteName(pitchEvent.MidiNote) == targetMidiNoteName)
        {
            long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
            long delayInMillis = currentTimeInMillis - currentIterationStartTimeInMillis;
            delaysInMillis.Add(delayInMillis);
            
            Debug.Log($"Mic delay calibration - correct pitch, delay of iteration {currentIteration}: {delayInMillis} ms");
            sineToneAudioGenerator.Stop();
            
            currentIteration++;
            if (currentIteration >= midiNoteNameAndFrequencies.Count)
            {
                OnEndCalibration();
            }
            else
            {
                // Wait a bit for silence before the next iteration.
                nextIterationStartTimeInMillis = currentTimeInMillis + PauseTimeInMillis;
                Debug.Log($"currentTime: {currentTimeInMillis}, nextIterationStartTimeInMillis: {nextIterationStartTimeInMillis}");
            }
        }
        else
        {
            Debug.Log("Mic delay calibration - wrong pitch: " + MidiUtils.GetAbsoluteName(pitchEvent.MidiNote));
        }
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
        public bool IsSuccess { get; set; }
        public List<long> DelaysInMilliseconds { get; set; }
    }
}
