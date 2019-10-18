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

    private double lastPitchDetectedBeat;

    private RecordedNote lastRecordedNote;

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

    public void OnPitchDetected(int midiNote)
    {
        double currentBeat = singSceneController.CurrentBeat;
        if (midiNote <= 0)
        {
            if (lastRecordedNote != null)
            {
                // End singing of last recorded note.
                // Do this seamlessly, i.e., continue the last recorded note until now.
                lastRecordedNote.EndBeat = currentBeat;
                OnContinuedNote(lastRecordedNote, currentBeat);
                lastRecordedNote = null;
                // Debug.Log("Ended singing");
            }
        }
        else
        {
            if (lastRecordedNote != null && lastRecordedNote.RecordedMidiNote == midiNote)
            {
                // Continue singing on same pitch
                lastRecordedNote.EndBeat = currentBeat;
                OnContinuedNote(lastRecordedNote, currentBeat);
                // Debug.Log("Continued note");
            }
            else
            {
                playerController.OnRecordedNoteEnded(lastRecordedNote);
                // Start new note.
                // Do this seamlessly, i.e., start the new note at the time the last note was recorded.
                lastRecordedNote = new RecordedNote(midiNote, lastPitchDetectedBeat, currentBeat);
                // Debug.Log("Start new note");
            }
        }
        lastPitchDetectedBeat = currentBeat;
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
        if (noteAtCurrentBeat.Type == ENoteType.Rap || noteAtCurrentBeat.Type == ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
            recordedNote.RoundedMidiNote = noteAtCurrentBeat.MidiNote;
        }
        else
        {
            // Round recorded note if it is close to the target note.
            recordedNote.RoundedMidiNote = GetRoundedMidiNote(recordedNote.RecordedMidiNote, noteAtCurrentBeat.MidiNote, RoundingDistance);
        }

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

    public static Note GetNoteAtBeat(Sentence sentence, double beat)
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
