using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IOnHotSwapFinishedListener
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
    private MicrophonePitchTracker microphonePitchTracker;

    // The rounding distance of the PlayerProfile's difficulty.
    private int roundingDistance;

    private RecordedNote lastRecordedNote;
    private RecordedNote lastEndedNote;
    private int nextNoteStartBeat;

    private double lastBeatWithPitchEvent;

    public void OnInjectionFinished()
    {
        roundingDistance = playerProfile.Difficulty.GetRoundingDistance();

        if (micProfile != null)
        {
            microphonePitchTracker.MicProfile = micProfile;
        }
    }

    public void SetMicrophonePitchTrackerEnabled(bool newValue)
    {
        microphonePitchTracker.enabled = newValue;
    }

    void Start()
    {
        if (micProfile != null)
        {
            microphonePitchTracker.StartPitchDetection();
            microphonePitchTracker.PitchEventStream.Subscribe(HandlePitchEvent);
        }
        else
        {
            Debug.LogWarning("No mic for player " + playerProfile.Name + ". Not recording player notes.");
        }
    }

    public void OnHotSwapFinished()
    {
        Start();
    }

    void OnDisable()
    {
        if (micProfile != null)
        {
            microphonePitchTracker.StopPitchDetection();
        }
    }

    public List<RecordedNote> GetRecordedNotes(Sentence sentence)
    {
        sentenceToRecordedNotesMap.TryGetValue(sentence, out List<RecordedNote> recordedNotes);
        return recordedNotes;
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

    public void HandlePitchEvent(PitchEvent pitchEvent)
    {
        double currentBeat = GetCurrentBeat();

        // It could be that some beats have been missed, for example because the frame rate was too low.
        // In this case, the pitch event is fired here also for the missed beats.
        if (lastBeatWithPitchEvent < currentBeat)
        {
            int missedBeats = (int)(currentBeat - lastBeatWithPitchEvent);
            for (int i = 1; i <= missedBeats; i++)
            {
                HandlePitchEvent(pitchEvent, lastBeatWithPitchEvent + i);
            }
        }
        HandlePitchEvent(pitchEvent, currentBeat);

        lastBeatWithPitchEvent = currentBeat;
    }

    private void HandlePitchEvent(PitchEvent pitchEvent, double currentBeat)
    {
        if (pitchEvent == null || pitchEvent.MidiNote <= 0)
        {
            if (lastRecordedNote != null)
            {
                HandleRecordedNoteEnded(currentBeat);
            }
        }
        else
        {
            if (lastRecordedNote != null)
            {
                if (MidiUtils.GetRelativePitchDistance(lastRecordedNote.RoundedMidiNote, pitchEvent.MidiNote) <= roundingDistance)
                {
                    // Continue singing on same pitch
                    HandleRecordedNoteContinued(currentBeat);
                }
                else
                {
                    // Continue singing on different pitch. Finish the last recorded note.
                    HandleRecordedNoteEnded(currentBeat);
                }
            }

            // The lastRecordedNote could be ended above, so the following null check is not redundant.
            if (lastRecordedNote == null && currentBeat >= nextNoteStartBeat)
            {
                // Start singing of a new note
                HandleRecordedNoteStarted(pitchEvent.MidiNote, currentBeat);
            }
        }
    }

    private void HandleRecordedNoteStarted(int midiNote, double currentBeat)
    {
        Sentence currentSentence = playerController.CurrentSentence;

        // Only accept recorded notes where a note is expected in the song
        Note noteAtCurrentBeat = GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat == null)
        {
            return;
        }

        // If the last note ended at the start of the new note, then continue using the last ended note.
        int roundedMidiNote = GetRoundedMidiNoteForRecordedNote(noteAtCurrentBeat, midiNote);
        double startBeat = Math.Floor(currentBeat);
        if (lastEndedNote != null
            && lastEndedNote.Sentence == currentSentence
            && lastEndedNote.EndBeat == startBeat
            && lastEndedNote.RoundedMidiNote == roundedMidiNote)
        {
            lastRecordedNote = lastEndedNote;
            HandleRecordedNoteContinued(currentBeat);
            return;
        }

        lastRecordedNote = new RecordedNote(midiNote, Math.Floor(currentBeat), currentBeat);
        // The note at the same beat is the target note that should be sung
        lastRecordedNote.TargetNote = noteAtCurrentBeat;
        lastRecordedNote.Sentence = currentSentence;
        lastRecordedNote.RoundedMidiNote = roundedMidiNote;

        // Remember this note
        AddRecordedNote(lastRecordedNote, currentSentence);
    }

    private void HandleRecordedNoteContinued(double currentBeat)
    {
        lastRecordedNote.EndBeat = currentBeat;

        bool targetNoteIsDone = (lastRecordedNote.TargetNote != null && lastRecordedNote.EndBeat >= lastRecordedNote.TargetNote.EndBeat);
        if (targetNoteIsDone)
        {
            lastRecordedNote.EndBeat = lastRecordedNote.TargetNote.EndBeat;
            playerController.OnRecordedNoteEnded(lastRecordedNote);
            lastRecordedNote = null;
        }
        else
        {
            playerController.OnRecordedNoteContinued(lastRecordedNote);
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
    }

    private int GetRoundedMidiNoteForRecordedNote(Note targetNote, int recordedMidiNote)
    {
        if (targetNote.Type == ENoteType.Rap || targetNote.Type == ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
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
}
