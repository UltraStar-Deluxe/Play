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

    // The rounding distance of the PlayerProfile
    private int roundingDistance;

    private int recordingSentenceIndex;
    public int BeatToAnalyze { get; private set; }

    public Sentence RecordingSentence { get; private set; }
    private List<Note> currentAndUpcomingNotesInRecordingSentence;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private Subject<BeatAnalyzedEvent> beatAnalyzedEventStream = new Subject<BeatAnalyzedEvent>();
    public IObservable<BeatAnalyzedEvent> BeatAnalyzedEventStream
    {
        get
        {
            return beatAnalyzedEventStream;
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

    void Start()
    {
        // Find first sentence to analyze
        SetRecordingSentence(recordingSentenceIndex);

        if (micProfile != null)
        {
            roundingDistance = playerProfile.Difficulty.GetRoundingDistance();
            audioSamplesAnalyzer = MicPitchTracker.CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm, micSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
            micSampleRecorder.MicProfile = micProfile;
            micSampleRecorder.StartRecording();
        }
        else
        {
            Debug.LogWarning($"No mic for player {playerProfile.Name}. Not recording player notes.");
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // No sentence to analyze left (all done).
        if (RecordingSentence == null)
        {
            return;
        }

        // Analyze the next beat with fully recorded mic samples
        double nextBeatToAnalyzeEndPositionInMs = BpmUtils.BeatToMillisecondsInSong(songMeta, BeatToAnalyze + 1);
        if (nextBeatToAnalyzeEndPositionInMs < songAudioPlayer.PositionInSongInMillis - micProfile.DelayInMillis)
        {
            // The beat has passed and should have recorded samples in the mic buffer. Analyze the samples now.
            int startSampleBufferIndex = GetMicSampleBufferIndexForBeat(BeatToAnalyze);
            int endSampleBufferIndex = GetMicSampleBufferIndexForBeat(BeatToAnalyze + 1);
            if (startSampleBufferIndex > endSampleBufferIndex)
            {
                ObjectUtils.Swap(ref startSampleBufferIndex, ref endSampleBufferIndex);
            }

            Note currentOrUpcomingNote = currentAndUpcomingNotesInRecordingSentence[0];
            Note noteAtBeat = (currentOrUpcomingNote.StartBeat <= BeatToAnalyze && BeatToAnalyze < currentOrUpcomingNote.EndBeat)
                ? currentOrUpcomingNote
                : null;

            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(micSampleRecorder.MicSamples, startSampleBufferIndex, endSampleBufferIndex, micProfile);
            FirePitchEvent(pitchEvent, BeatToAnalyze, noteAtBeat);

            GoToNextBeat();
        }
    }

    public void FirePitchEvent(PitchEvent pitchEvent, int beat, Note noteAtBeat)
    {
        int roundedMidiNote = pitchEvent != null
            ? GetRoundedMidiNoteForRecordedMidiNote(noteAtBeat, pitchEvent.MidiNote)
            : -1;
        beatAnalyzedEventStream.OnNext(new BeatAnalyzedEvent(pitchEvent, beat, noteAtBeat, roundedMidiNote));
    }

    public void GoToNextBeat()
    {
        BeatToAnalyze++;
        if (BeatToAnalyze > RecordingSentence.MaxBeat)
        {
            // All beats of the sentence analyzed. Go to next sentence.
            GoToNextRecordingSentence();
            return;
        }

        // If there is no note at that beat, then use the StartBeat of the following note for next analysis.
        // Remove notes that have been completely analyzed.
        Note passedNote = null;
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty()
            && currentAndUpcomingNotesInRecordingSentence[0].EndBeat <= BeatToAnalyze)
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
            if (currentOrUpcomingNote.StartBeat > BeatToAnalyze)
            {
                // Next beat to analyze is at the next note
                BeatToAnalyze = currentOrUpcomingNote.StartBeat;
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
            BeatToAnalyze = 0;
            return;
        }
        currentAndUpcomingNotesInRecordingSentence = SongMetaUtils.GetSortedNotes(RecordingSentence);

        BeatToAnalyze = RecordingSentence.MinBeat;
    }

    void OnDisable()
    {
        if (micProfile != null)
        {
            micSampleRecorder.StopRecording();
        }
    }

    private int GetRoundedMidiNoteForRecordedMidiNote(Note targetNote, int recordedMidiNote)
    {
        if (targetNote.Type == ENoteType.Rap || targetNote.Type == ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
            return targetNote.MidiNote;
        }
        else if (recordedMidiNote < MidiUtils.SingableNoteMin || recordedMidiNote > MidiUtils.SingableNoteMax)
        {
            // The pitch detection can fail, which is the case when the detected pitch is outside of the singable note range.
            // In this case, just assume that the player was singing correctly and round to the target note.
            return targetNote.MidiNote;
        }
        else
        {
            // Round recorded note if it is close to the target note.
            return GetRoundedMidiNote(recordedMidiNote, targetNote.MidiNote, roundingDistance);
        }
    }

    private int GetRoundedMidiNote(int recordedMidiNote, int targetMidiNote, int roundingDistance)
    {
        int distance = MidiUtils.GetRelativePitchDistance(recordedMidiNote, targetMidiNote);
        if (distance <= roundingDistance)
        {
            return targetMidiNote;
        }
        else
        {
            return recordedMidiNote;
        }
    }

    public class BeatAnalyzedEvent
    {
        public PitchEvent PitchEvent { get; private set; }
        public int Beat { get; private set; }
        public Note NoteAtBeat { get; private set; }
        public int RoundedMidiNote { get; private set; }

        public BeatAnalyzedEvent(PitchEvent pitchEvent, int beat, Note noteAtBeat, int roundedMidiNote)
        {
            PitchEvent = pitchEvent;
            Beat = beat;
            NoteAtBeat = noteAtBeat;
            RoundedMidiNote = roundedMidiNote;
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
