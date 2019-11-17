using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pitch;
using UnityEngine;
using UniRx;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour
{
    public Dictionary<Sentence, List<RecordedNote>> sentenceToRecordedNotesMap = new Dictionary<Sentence, List<RecordedNote>>();

    private SingSceneController singSceneController;

    private PlayerController playerController;

    private int RoundingDistance { get; set; }

    private double lastPitchDetectedBeat;

    private RecordedNote lastRecordedNote;

    private PlayerProfile playerProfile;
    private MicProfile micProfile;

    private bool wasStartedAlready;

    private IDisposable pitchEventStreamDisposable;

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
            MicrophonePitchTracker.MicDevice = micProfile.Name;
        }
    }

    void Awake()
    {
        singSceneController = GameObject.FindObjectOfType<SingSceneController>();
    }

    void OnEnable()
    {
        // Start is not called after hot-swap, but OnEnable is called before the Init method (before Instantiate(...) returns).
        // However, if OnEnable is called after the object has been initialized, then we know we are called after hot-swap.
        // TODO: Introduce a new common base class on top of MonoBehaviour that handles this.
        if (wasStartedAlready)
        {
            // This is called after hot-swap, because Start has been called before and we are in OnEnable.
            Start();
        }
    }

    void Start()
    {
        wasStartedAlready = true;
        if (micProfile != null)
        {
            pitchEventStreamDisposable = MicrophonePitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
            MicrophonePitchTracker.StartPitchDetection();
        }
        else
        {
            Debug.LogWarning("No mic for player " + playerProfile.Name + ". Not recording player notes.");
        }
    }

    void OnDisable()
    {
        if (micProfile != null)
        {
            pitchEventStreamDisposable?.Dispose();
            MicrophonePitchTracker.StopPitchDetection();
        }
    }

    public List<RecordedNote> GetRecordedNotes(Sentence sentence)
    {
        sentenceToRecordedNotesMap.TryGetValue(sentence, out List<RecordedNote> recordedNotes);
        return recordedNotes;
    }

    public void OnPitchDetected(PitchEvent pitchEvent)
    {
        int midiNote = pitchEvent.MidiNote;
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
