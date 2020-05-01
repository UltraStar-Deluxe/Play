using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniInject;
using System.Linq;
using CSharpSynth.Wave;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Takes the analyzed pitches from the PlayerPitchTracker and creates display events to draw recorded notes.
// For example, multiple beats next to each other can be considered as one note.
[RequireComponent(typeof(PlayerPitchTracker))]
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
    private PlayerPitchTracker playerPitchTracker;

    // The rounding distance of the PlayerProfile's difficulty.
    private int roundingDistance;

    private RecordedNote lastRecordedNote;
    private RecordedNote lastEndedNote;
    private int nextNoteStartBeat;

    // The joker is used to cancel a mistake in continued singing.
    // The joker is earned for continued singing of the correct pitch.
    private int availableJokerCount;
    private const int MaxJokerCount = 1;

    // For debugging only: see how many jokers have been used in the inspector
    [ReadOnly]
    public int usedJokerCount;
    // For debugging only: see how many times it was rounded to the target note because the pitch detection failed
    [ReadOnly]
    public int roundingBecausePitchDetectionFailedCount;

    public void OnInjectionFinished()
    {
        roundingDistance = playerProfile.Difficulty.GetRoundingDistance();
        playerPitchTracker.BeatAnalyzedEventStream
            .Subscribe(beatAnalyzedEvent => HandlePitchEvent(beatAnalyzedEvent.PitchEvent, beatAnalyzedEvent.Beat, true));
    }

    public void SetEnabled(bool newValue)
    {
        playerPitchTracker.SetEnabled(newValue);
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
        Note noteAtCurrentBeat = GetNoteAtBeat(playerPitchTracker.RecordingSentence, currentBeat);
        if (noteAtCurrentBeat == null)
        {
            return;
        }

        // If the last note ended at the start of the new note and can be further extended,
        // then continue using the last ended note.
        int roundedMidiNote = GetRoundedMidiNoteForRecordedNote(noteAtCurrentBeat, midiNote);
        double startBeat = Math.Floor(currentBeat);
        if (lastEndedNote != null
            && lastEndedNote.Sentence == playerPitchTracker.RecordingSentence
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
        lastRecordedNote.Sentence = playerPitchTracker.RecordingSentence;
        lastRecordedNote.RoundedMidiNote = roundedMidiNote;

        // Remember this note
        AddRecordedNote(lastRecordedNote, playerPitchTracker.RecordingSentence);
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
