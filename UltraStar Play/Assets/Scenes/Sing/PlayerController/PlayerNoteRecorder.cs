using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniInject;
using System.Linq;
using CSharpSynth.Wave;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(MicSampleRecorder))]
public class PlayerNoteRecorder : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    public Dictionary<Sentence, List<RecordedNote>> sentenceToRecordedNotesMap = new Dictionary<Sentence, List<RecordedNote>>();

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
    private SingSceneController singSceneController;

    [Inject]
    private Settings settings;

    // The rounding distance of the PlayerProfile's difficulty.
    private int roundingDistance;

    private RecordedNote lastRecordedNote;
    private RecordedNote lastEndedNote;
    private int nextNoteStartBeat;

    private int recordingSentenceIndex;
    private int nextBeatToAnalyze;

    // The joker is used to cancel a mistake in continued singing.
    // The joker is earned for continued singing of the correct pitch.
    private int availableJokerCount;
    private const int MaxJokerCount = 1;

    public Sentence RecordingSentence { get; private set; }
    private List<Note> currentAndUpcomingNotesInRecordingSentence;

    // For debugging only: see how many jokers have been used in the inspector
    [ReadOnly]
    public int usedJokerCount;
    // For debugging only: see how many times it was rounded to the target note because the pitch detection failed
    [ReadOnly]
    public int roundingBecausePitchDetectionFailedCount;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    public void OnInjectionFinished()
    {
        roundingDistance = playerProfile.Difficulty.GetRoundingDistance();

        if (micProfile != null)
        {
            micSampleRecorder.MicProfile = micProfile;
        }
    }

    public void SetMicrophonePitchTrackerEnabled(bool newValue)
    {
        micSampleRecorder.enabled = newValue;
    }

    void Start()
    {
        if (micProfile != null)
        {
            audioSamplesAnalyzer = MicPitchTracker.CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm, micSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
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
        double nextBeatToAnalyzeEndPositionInMs = BpmUtils.BeatToMillisecondsInSong(songMeta, nextBeatToAnalyze + 1);
        if (nextBeatToAnalyzeEndPositionInMs < songAudioPlayer.PositionInSongInMillis - micProfile.DelayInMillis)
        {
            // The beat has passed and should have recorded samples in the mic buffer. Analyze the samples now.
            int startSampleBufferIndex = GetMicSampleBufferIndexForBeat(nextBeatToAnalyze);
            int endSampleBufferIndex = GetMicSampleBufferIndexForBeat(nextBeatToAnalyze + 1);
            if (startSampleBufferIndex > endSampleBufferIndex)
            {
                ObjectUtils.Swap(ref startSampleBufferIndex, ref endSampleBufferIndex);
            }
            //int sampleLength = endSampleBufferIndex - startSampleBufferIndex;

            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(micSampleRecorder.MicSamples, startSampleBufferIndex, endSampleBufferIndex, micProfile);

            HandlePitchEvent(pitchEvent, nextBeatToAnalyze, true);

            FindNextBeatToAnalyze();
        }
    }

    private void FindNextBeatToAnalyze()
    {
        nextBeatToAnalyze++;
        if (nextBeatToAnalyze > RecordingSentence.MaxBeat)
        {
            // All beats of the sentence analyzed. Go to next sentence.
            recordingSentenceIndex++;
            SetRecordingSentence(recordingSentenceIndex);
            return;
        }

        // If there is no note at that beat, then use the StartBeat of the following note for next analysis.
        // Remove notes that have been completely analyzed.
        while (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty()
            && currentAndUpcomingNotesInRecordingSentence[0].EndBeat <= nextBeatToAnalyze)
        {
            currentAndUpcomingNotesInRecordingSentence.RemoveAt(0);
        }
        // Check if there is still a current note that is analyzed. If not, skip to the next upcoming note.
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty())
        {
            Note currentOrUpcomingNote = currentAndUpcomingNotesInRecordingSentence[0];
            if (currentOrUpcomingNote.StartBeat > nextBeatToAnalyze)
            {
                // This note is upcoming, thus there is no current note to be analyzed anymore.
                nextBeatToAnalyze = currentOrUpcomingNote.StartBeat;
            }
        }
        else
        {
            // All notes of the sentence analyzed. Go to next sentence.
            recordingSentenceIndex++;
            SetRecordingSentence(recordingSentenceIndex);
            return;
        }
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
            nextBeatToAnalyze = 0;
            return;
        }
        currentAndUpcomingNotesInRecordingSentence = SongMetaUtils.GetSortedNotes(RecordingSentence);

        nextBeatToAnalyze = RecordingSentence.MinBeat;
    }

    void OnDisable()
    {
        if (micProfile != null)
        {
            micSampleRecorder.StopRecording();
        }
    }

    public List<RecordedNote> GetRecordedNotes(Sentence sentence)
    {
        sentenceToRecordedNotesMap.TryGetValue(sentence, out List<RecordedNote> recordedNotes);
        return recordedNotes;
    }

    public void RemoveRecordedNotes(Sentence sentence)
    {
        sentenceToRecordedNotesMap.Remove(sentence);
    }

    public void OnSentenceEnded()
    {
        // Finish the last note.
        if (lastRecordedNote != null)
        {
            double currentBeat = GetCurrentBeat();
            HandleRecordedNoteEnded(currentBeat);
        }
    }

    public void HandlePitchEvent(PitchEvent pitchEvent, double currentBeat, bool updateUi)
    {
        // Stop recording
        if (pitchEvent == null || pitchEvent.MidiNote <= 0)
        {
            if (lastRecordedNote != null)
            {
                HandleRecordedNoteEnded(currentBeat);
            }
            return;
        }

        // Start new recorded note
        if (lastRecordedNote == null)
        {
            HandleRecordedNoteStarted(pitchEvent.MidiNote, currentBeat, updateUi);
            return;
        }

        // Continue or finish existing recorded note. Possibly starting new note to change pitch.
        bool isTargetNoteHitNow = MidiUtils.GetRelativePitchDistance(lastRecordedNote.TargetNote.MidiNote, pitchEvent.MidiNote) <= roundingDistance;
        if (isTargetNoteHitNow && !IsTargetNoteHit(lastRecordedNote))
        {
            // Jump from a wrong pitch to correct pitch.
            // Otherwise, the rounding could tend towards the wrong pitch
            // when the player starts a note with the wrong pitch.
            HandleRecordedNoteEnded(currentBeat);
            HandleRecordedNoteStarted(pitchEvent.MidiNote, currentBeat, updateUi);
        }
        else if (MidiUtils.GetRelativePitchDistance(lastRecordedNote.RoundedMidiNote, pitchEvent.MidiNote) <= roundingDistance)
        {
            // Earned a joker for continued correct singing.
            if (IsTargetNoteHit(lastRecordedNote) && availableJokerCount < MaxJokerCount)
            {
                availableJokerCount++;
            }

            // Continue singing on same pitch
            HandleRecordedNoteContinued(pitchEvent.MidiNote, currentBeat, updateUi);
        }
        else
        {
            // Changed pitch while singing.
            if (!isTargetNoteHitNow
                && IsTargetNoteHit(lastRecordedNote)
                && availableJokerCount > 0)
            {
                // Because of the joker, this beat is still counted as correct although it is not. The joker is gone.
                availableJokerCount--;
                usedJokerCount++;
                HandleRecordedNoteContinued(lastRecordedNote.RecordedMidiNote, currentBeat, updateUi);
            }
            else
            {
                // Continue singing on different pitch.
                HandleRecordedNoteEnded(currentBeat);
                HandleRecordedNoteStarted(pitchEvent.MidiNote, currentBeat, updateUi);
            }
        }
    }

    private void HandleRecordedNoteStarted(int midiNote, double currentBeat, bool updateUi)
    {
        if (currentBeat < nextNoteStartBeat)
        {
            return;
        }

        // Only accept recorded notes where a note is expected in the song
        Note noteAtCurrentBeat = GetNoteAtBeat(RecordingSentence, currentBeat);
        if (noteAtCurrentBeat == null)
        {
            return;
        }

        // If the last note ended at the start of the new note and can be further extended,
        // then continue using the last ended note.
        int roundedMidiNote = GetRoundedMidiNoteForRecordedNote(noteAtCurrentBeat, midiNote);
        double startBeat = Math.Floor(currentBeat);
        if (lastEndedNote != null
            && lastEndedNote.Sentence == RecordingSentence
            && lastEndedNote.EndBeat == startBeat
            && lastEndedNote.RoundedMidiNote == roundedMidiNote
            && lastEndedNote.TargetNote.EndBeat > lastEndedNote.EndBeat)
        {
            lastRecordedNote = lastEndedNote;
            HandleRecordedNoteContinued(midiNote, currentBeat, updateUi);
            return;
        }

        lastRecordedNote = new RecordedNote(midiNote, Math.Floor(currentBeat), currentBeat);
        // The note at the same beat is the target note that should be sung
        lastRecordedNote.TargetNote = noteAtCurrentBeat;
        lastRecordedNote.Sentence = RecordingSentence;
        lastRecordedNote.RoundedMidiNote = roundedMidiNote;

        // Remember this note
        AddRecordedNote(lastRecordedNote, RecordingSentence);
    }

    private void HandleRecordedNoteContinued(int midiNote, double currentBeat, bool updateUi)
    {
        lastRecordedNote.EndBeat = currentBeat;

        bool targetNoteIsDone = (lastRecordedNote.TargetNote != null && lastRecordedNote.EndBeat >= lastRecordedNote.TargetNote.EndBeat);
        if (targetNoteIsDone)
        {
            lastRecordedNote.EndBeat = lastRecordedNote.TargetNote.EndBeat;
            playerController.OnRecordedNoteEnded(lastRecordedNote);
            lastRecordedNote = null;

            HandleRecordedNoteStarted(midiNote, currentBeat, updateUi);
        }
        else
        {
            playerController.OnRecordedNoteContinued(lastRecordedNote, updateUi);
        }
    }

    private void HandleRecordedNoteEnded(double currentBeat)
    {
        // Extend the note to the end of the beat
        lastRecordedNote.EndBeat = Math.Ceiling(currentBeat);
        if (lastRecordedNote.TargetNote != null
            && lastRecordedNote.EndBeat > lastRecordedNote.TargetNote.EndBeat)
        {
            lastRecordedNote.EndBeat = lastRecordedNote.TargetNote.EndBeat;
        }

        // The next note can be recorded starting from the next beat.
        nextNoteStartBeat = (int)lastRecordedNote.EndBeat;

        playerController.OnRecordedNoteEnded(lastRecordedNote);
        lastEndedNote = lastRecordedNote;
        lastRecordedNote = null;
        availableJokerCount = 0;
    }

    private int GetRoundedMidiNoteForRecordedNote(Note targetNote, int recordedMidiNote)
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
            roundingBecausePitchDetectionFailedCount++;
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

    public static Note GetNoteAtBeat(Sentence sentence, double beat)
    {
        if (sentence == null)
        {
            return null;
        }

        foreach (Note note in sentence.Notes)
        {
            if (beat >= note.StartBeat && beat <= note.EndBeat)
            {
                return note;
            }
        }
        return null;
    }

    private void AddRecordedNote(RecordedNote recordedNote, Sentence currentSentence)
    {
        // Add new recorded note to collection of recorded notes that is associated with the sentence.
        // Thereby, construct collections of recorded notes if needed and associate it with the sentence.
        if (sentenceToRecordedNotesMap.TryGetValue(currentSentence, out List<RecordedNote> recordedNotes))
        {
            recordedNotes.AddIfNotContains(recordedNote);
        }
        else
        {
            recordedNotes = new List<RecordedNote>();
            recordedNotes.Add(recordedNote);
            sentenceToRecordedNotesMap.Add(currentSentence, recordedNotes);
        }
    }

    private double GetCurrentBeat()
    {
        double positionInMillis = songAudioPlayer.PositionInSongInMillis;
        if (micProfile != null)
        {
            positionInMillis -= micProfile.DelayInMillis;
        }
        double currentBeat = BpmUtils.MillisecondInSongToBeat(songMeta, positionInMillis);
        return currentBeat;
    }

    private bool IsTargetNoteHit(RecordedNote recordedNote)
    {
        return recordedNote.TargetNote != null && recordedNote.TargetNote.MidiNote == recordedNote.RoundedMidiNote;
    }
}
