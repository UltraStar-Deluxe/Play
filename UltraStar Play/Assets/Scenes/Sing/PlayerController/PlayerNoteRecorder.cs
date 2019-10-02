using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pitch;
using UnityEngine;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour
{
    public Dictionary<Sentence, List<RecordedNote>> sentenceToRecordedNotesMap = new Dictionary<Sentence, List<RecordedNote>>();

    private SingSceneController singSceneController;

    private PlayerController playerController;

    private PlayerProfile playerProfile;
    public PlayerProfile PlayerProfile
    {
        get
        {
            return playerProfile;
        }
        set
        {
            playerProfile = value;
            MicrophonePitchTracker.MicDevice = playerProfile.MicDevice;
        }
    }

    private int RoundingDistance { get; set; }

    private RecordedNote lastRecordedNote;
    private int lastRecordedFrame;

    private MicrophonePitchTracker MicrophonePitchTracker
    {
        get
        {
            return GetComponent<MicrophonePitchTracker>();
        }
    }

    public void Init(PlayerController playerController, int roundingDistance)
    {
        this.playerController = playerController;
        this.RoundingDistance = roundingDistance;
    }

    void Awake()
    {
        singSceneController = GameObject.FindObjectOfType<SingSceneController>();

        if (playerProfile == null)
        {
            playerProfile = PlayerProfileManager.Instance.PlayerProfiles[0];
        }
    }

    void OnEnable()
    {
        MicrophonePitchTracker.MicDevice = playerProfile.MicDevice;
        MicrophonePitchTracker.AddPitchDetectedHandler(OnPitchDetected);
        MicrophonePitchTracker.StartPitchDetection();
    }

    void OnDisable()
    {
        MicrophonePitchTracker.RemovePitchDetectedHandler(OnPitchDetected);
        MicrophonePitchTracker.StopPitchDetection();
    }

    public List<RecordedNote> GetRecordedNotes(Sentence sentence)
    {
        sentenceToRecordedNotesMap.TryGetValue(sentence, out List<RecordedNote> recordedNotes);
        return recordedNotes;
    }

    private void OnPitchDetected(PitchTracker sender, PitchTracker.PitchRecord pitchRecord)
    {
        // Ignore multiple events at same frame
        if (lastRecordedFrame == Time.frameCount)
        {
            return;
        }
        lastRecordedFrame = Time.frameCount;

        if (pitchRecord.MidiNote <= 0)
        {
            if (lastRecordedNote != null)
            {
                // Ended singing
                lastRecordedNote = null;
            }
        }
        else
        {
            double currentPositionInMillis = singSceneController.PositionInSongInMillis;
            double currentBeat = singSceneController.CurrentBeat;
            if (lastRecordedNote != null && lastRecordedNote.RecordedMidiNote == pitchRecord.MidiNote)
            {
                // Continued singing on same pitch
                lastRecordedNote.EndPositionInMilliseconds = currentPositionInMillis;
                lastRecordedNote.EndBeat = currentBeat;
                OnContinuedNote(lastRecordedNote, currentBeat);
            }
            else
            {
                // Start new note
                lastRecordedNote = new RecordedNote(pitchRecord.MidiNote, currentPositionInMillis, currentPositionInMillis, currentBeat, currentBeat);
            }
        }
    }

    private void OnContinuedNote(RecordedNote recordedNote, double currentBeat)
    {
        if (playerController == null)
        {
            lastRecordedNote = null;
            return;
        }
        Sentence currentSentence = playerController.CurrentSentence;
        if (currentSentence == null)
        {
            lastRecordedNote = null;
            return;
        }

        // Only accept recorded notes where a note is expected in the song
        Note noteAtCurrentBeat = GetNoteAtBeat(currentSentence, currentBeat);
        if (noteAtCurrentBeat == null)
        {
            lastRecordedNote = null;
            return;
        }

        // Limit recorded note bounds to target note bounds
        if (recordedNote.StartBeat < noteAtCurrentBeat.StartBeat)
        {
            recordedNote.StartBeat = noteAtCurrentBeat.StartBeat;
        }
        if (recordedNote.EndBeat > noteAtCurrentBeat.EndBeat)
        {
            recordedNote.EndBeat = noteAtCurrentBeat.EndBeat;
        }

        // Round pitch of recorded note to pitch of note in song
        recordedNote.RoundedMidiNote = GetRoundedMidiNote(recordedNote.RecordedMidiNote, noteAtCurrentBeat.MidiNote, RoundingDistance);

        // Remember this note and show it in the UI
        AddRecordedNote(lastRecordedNote, currentSentence);
        playerController.DisplayRecordedNotes(GetRecordedNotes(currentSentence));
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

    private Note GetNoteAtBeat(Sentence sentence, double beat)
    {
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
            if (!recordedNotes.Contains(recordedNote))
            {
                recordedNotes.Add(recordedNote);
            }
        }
        else
        {
            recordedNotes = new List<RecordedNote>();
            recordedNotes.Add(recordedNote);
            sentenceToRecordedNotesMap.Add(currentSentence, recordedNotes);
        }
    }
}
