using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniInject;
using System.Linq;
using CSharpSynth.Wave;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Analyzes the recorded mic input of a player to find the pitch for beats in the song.
[RequireComponent(typeof(MicSampleRecorder))]
public partial class PlayerPitchTracker : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private PlayerController playerController;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(optional = true)]
    private MicProfile micProfile;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private MicSampleRecorder micSampleRecorder;

    [Inject]
    private Settings settings;

    private int recordingSentenceIndex;
    private int beatToAnalyze;

    public Sentence RecordingSentence { get; private set; }
    private List<Note> currentAndUpcomingNotesInRecordingSentence;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private Subject<BeatAnalyzedEvent> pitchEventStream = new Subject<BeatAnalyzedEvent>();
    public IObservable<BeatAnalyzedEvent> PitchEventStream
    {
        get
        {
            return pitchEventStream;
        }
    }

    private Subject<NoteAnalyzedEvent> noteAnalyzedEventStream = new Subject<NoteAnalyzedEvent>();
    public IObservable<NoteAnalyzedEvent> NoteAnalyzedEventStream
    {
        get
        {
            return noteAnalyzedEventStream;
        }
    }

    private Subject<SentenceAnalyzedEvent> sentenceAnalyzedEventStream = new Subject<SentenceAnalyzedEvent>();
    public IObservable<SentenceAnalyzedEvent> SentenceAnalyzedEventStream
    {
        get
        {
            return sentenceAnalyzedEventStream;
        }
    }

    public void SetEnabled(bool newValue)
    {
        micSampleRecorder.enabled = newValue;
    }

    void Start()
    {
        if (micProfile != null)
        {
            audioSamplesAnalyzer = MicPitchTracker.CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm, micSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
            micSampleRecorder.MicProfile = micProfile;
            micSampleRecorder.StartRecording();
        }
        else
        {
            Debug.LogWarning($"No mic for player ${playerProfile.Name}. Not recording player notes.");
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Find first sentence to analyze
        if (recordingSentenceIndex == 0 && RecordingSentence == null)
        {
            SetRecordingSentence(recordingSentenceIndex);
        }

        // No sentence to analyze left (all done).
        if (RecordingSentence == null)
        {
            return;
        }

        // Analyze the next beat with fully recorded mic samples
        double nextBeatToAnalyzeEndPositionInMs = BpmUtils.BeatToMillisecondsInSong(songMeta, beatToAnalyze + 1);
        if (nextBeatToAnalyzeEndPositionInMs < songAudioPlayer.PositionInSongInMillis - micProfile.DelayInMillis)
        {
            // The beat has passed and should have recorded samples in the mic buffer. Analyze the samples now.
            int startSampleBufferIndex = GetMicSampleBufferIndexForBeat(beatToAnalyze);
            int endSampleBufferIndex = GetMicSampleBufferIndexForBeat(beatToAnalyze + 1);
            if (startSampleBufferIndex > endSampleBufferIndex)
            {
                ObjectUtils.Swap(ref startSampleBufferIndex, ref endSampleBufferIndex);
            }

            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(micSampleRecorder.MicSamples, startSampleBufferIndex, endSampleBufferIndex, micProfile);
            pitchEventStream.OnNext(new BeatAnalyzedEvent(pitchEvent, beatToAnalyze));

            FindNextBeatToAnalyze();
        }
    }

    private void FindNextBeatToAnalyze()
    {
        beatToAnalyze++;
        if (beatToAnalyze > RecordingSentence.MaxBeat)
        {
            // All beats of the sentence analyzed. Go to next sentence.
            GoToNextRecordingSentence();
            return;
        }

        // If there is no note at that beat, then use the StartBeat of the following note for next analysis.
        // Remove notes that have been completely analyzed.
        Note passedNote = null;
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty()
            && currentAndUpcomingNotesInRecordingSentence[0].EndBeat <= beatToAnalyze)
        {
            passedNote = currentAndUpcomingNotesInRecordingSentence[0];
            currentAndUpcomingNotesInRecordingSentence.RemoveAt(0);
        }
        if (passedNote != null)
        {
            noteAnalyzedEventStream.OnNext(new NoteAnalyzedEvent(passedNote));
        }

        // Check if there is still a current note that is analyzed. If not, skip to the next upcoming note.
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty())
        {
            Note currentOrUpcomingNote = currentAndUpcomingNotesInRecordingSentence[0];
            if (currentOrUpcomingNote.StartBeat > beatToAnalyze)
            {
                // Next beat to analyze is at the next note
                beatToAnalyze = currentOrUpcomingNote.StartBeat;
            }
        }
        else
        {
            // All notes of the sentence analyzed. Go to next sentence.
            GoToNextRecordingSentence();
            return;
        }
    }

    private void GoToNextRecordingSentence()
    {
        // Fire event about finished sentence
        Sentence nextRecordingSentence = playerController.GetSentence(recordingSentenceIndex + 1);
        sentenceAnalyzedEventStream.OnNext(new SentenceAnalyzedEvent(RecordingSentence, nextRecordingSentence == null));
        // Select next sentence
        recordingSentenceIndex++;
        SetRecordingSentence(recordingSentenceIndex);
    }

    private int GetMicSampleBufferIndexForBeat(int beat)
    {
        double beatInMs = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        double beatPassedBeforeMs = songAudioPlayer.PositionInSongInMillis - beatInMs;
        int beatPassedBeforeSamplesInMicBuffer = Convert.ToInt32(((beatPassedBeforeMs - micProfile.DelayInMillis) / 1000) * micSampleRecorder.SampleRateHz);
        // The newest sample has the highest index in the MicSampleBuffer
        int sampleBufferIndex = micSampleRecorder.MicSamples.Length - beatPassedBeforeSamplesInMicBuffer;
        sampleBufferIndex = NumberUtils.Limit(sampleBufferIndex, 0, micSampleRecorder.MicSamples.Length - 1);
        return sampleBufferIndex;
    }

    private void SetRecordingSentence(int sentenceIndex)
    {
        RecordingSentence = playerController.GetSentence(sentenceIndex);
        if (RecordingSentence == null)
        {
            currentAndUpcomingNotesInRecordingSentence = new List<Note>();
            beatToAnalyze = 0;
            return;
        }
        currentAndUpcomingNotesInRecordingSentence = SongMetaUtils.GetSortedNotes(RecordingSentence);

        beatToAnalyze = RecordingSentence.MinBeat;
    }

    void OnDisable()
    {
        if (micProfile != null)
        {
            micSampleRecorder.StopRecording();
        }
    }

    public class BeatAnalyzedEvent
    {
        public PitchEvent PitchEvent { get; private set; }
        public int Beat { get; private set; }

        public BeatAnalyzedEvent(PitchEvent pitchEvent, int beat)
        {
            PitchEvent = pitchEvent;
            Beat = beat;
        }
    }

    public class NoteAnalyzedEvent
    {
        public Note Note { get; private set; }

        public NoteAnalyzedEvent(Note note)
        {
            Note = note;
        }
    }

    public class SentenceAnalyzedEvent
    {
        public Sentence Sentence { get; private set; }
        public bool IsLastSentence { get; private set; }

        public SentenceAnalyzedEvent(Sentence sentence, bool isLastSentence)
        {
            Sentence = sentence;
            IsLastSentence = isLastSentence;
        }
    }
}
