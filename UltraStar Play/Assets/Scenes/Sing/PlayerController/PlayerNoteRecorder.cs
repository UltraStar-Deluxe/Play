using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour, IOnHotSwapFinishedListener
{
    public Dictionary<Sentence, List<RecordedNote>> sentenceToRecordedNotesMap = new Dictionary<Sentence, List<RecordedNote>>();

    private SingSceneController singSceneController;

    private PlayerController playerController;

    private int RoundingDistance { get; set; }

    private RecordedNote lastRecordedNote;
    private RecordedNote lastEndedNote;
    private int nextNoteStartBeat;

    private PlayerProfile playerProfile;
    private MicProfile micProfile;

    private double lastBeat;

    private MicrophonePitchTracker MicrophonePitchTracker
    {
        get
        {
            return GetComponent<MicrophonePitchTracker>();
        }
    }

    public void Init(PlayerController playerController, PlayerProfile playerProfile, MicProfile micProfile)
    {
        this.playerController = playerController;
        this.playerProfile = playerProfile;
        this.micProfile = micProfile;

        RoundingDistance = playerProfile.Difficulty.GetRoundingDistance();

        if (micProfile != null)
        {
            MicrophonePitchTracker.MicProfile = micProfile;
        }
    }

    public void SetMicrophonePitchTrackerEnabled(bool newValue)
    {
        MicrophonePitchTracker.enabled = newValue;
    }

    void Awake()
    {
        singSceneController = GameObject.FindObjectOfType<SingSceneController>();
    }

    void Start()
    {
        if (micProfile != null)
        {
            MicrophonePitchTracker.StartPitchDetection();
            MicrophonePitchTracker.PitchEventStream.Subscribe(HandlePitchEvent);
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
            MicrophonePitchTracker.StopPitchDetection();
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
            double currentBeat = singSceneController.CurrentBeat;
            HandleRecordedNoteEnded(currentBeat);
        }
    }

    public void HandlePitchEvent(PitchEvent pitchEvent)
    {
        double currentBeat = singSceneController.CurrentBeat;

        // It could be that some beats have been missed, for example because the frame rate was too low.
        // In this case, the pitch event is fired here also for the missed beats.
        if (lastBeat < currentBeat)
        {
            int missedBeats = (int)(currentBeat - lastBeat);
            if (missedBeats > 0)
            {
                for (int i = 1; i <= missedBeats; i++)
                {
                    HandlePitchEvent(pitchEvent, lastBeat + i);
                }
            }
        }
        HandlePitchEvent(pitchEvent, currentBeat);

        lastBeat = currentBeat;
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
                if (MidiUtils.GetRelativePitchDistance(lastRecordedNote.RoundedMidiNote, pitchEvent.MidiNote) <= RoundingDistance)
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
            return GetRoundedMidiNote(recordedMidiNote, targetNote.MidiNote, RoundingDistance);
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
}
