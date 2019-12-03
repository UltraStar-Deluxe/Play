using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour, IOnHotSwapFinishedListener
{
    public Dictionary<Sentence, List<RecordedNote>> sentenceToRecordedNotesMap = new Dictionary<Sentence, List<RecordedNote>>();

    private SingSceneController singSceneController;

    private PlayerController playerController;

    private int RoundingDistance { get; set; }

    private double lastPitchDetectedBeat;

    private RecordedNote lastRecordedNote;

    private PlayerProfile playerProfile;
    private MicProfile micProfile;

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
            pitchEventStreamDisposable = MicrophonePitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
            MicrophonePitchTracker.StartPitchDetection();
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
                HandleEndOfLastRecordedNote(currentBeat);
                // Debug.Log("Ended singing");
            }
        }
        else
        {
            if (lastRecordedNote != null)
            {
                if (lastRecordedNote.RecordedMidiNote == midiNote)
                {
                    // Continue singing on same pitch
                    lastRecordedNote.EndBeat = currentBeat;
                    HandleContinuedNote(currentBeat);
                    // Debug.Log("Continued note");
                }
                else
                {
                    // Continue singing on different pitch.
                    // Do this seamlessly, i.e., continue the last recorded note until now
                    // and start the new note at the time the last note was recorded.
                    HandleEndOfLastRecordedNote(currentBeat);
                    lastRecordedNote = new RecordedNote(midiNote, lastPitchDetectedBeat, currentBeat);
                    // Debug.Log("Start new note (continued singing)");
                }
            }
            else
            {
                // Start singing
                lastRecordedNote = new RecordedNote(midiNote, lastPitchDetectedBeat, currentBeat);
                // Debug.Log("Start new note (started singing)");
            }
        }
        lastPitchDetectedBeat = currentBeat;
    }

    private void HandleEndOfLastRecordedNote(double currentBeat)
    {
        // End the note seamlessly, i.e., continue the last recorded note until now.
        lastRecordedNote.EndBeat = currentBeat;
        LimitRecordedNoteBoundsToTargetNoteBounds(lastRecordedNote);
        playerController.OnRecordedNoteEnded(lastRecordedNote);
        lastRecordedNote = null;
    }

    private void HandleContinuedNote(double currentBeat)
    {
        if (lastRecordedNote == null)
        {
            return;
        }

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

        // The note at the same beat is the target note that should be sung
        lastRecordedNote.TargetNote = noteAtCurrentBeat;
        LimitRecordedNoteBoundsToTargetNoteBounds(lastRecordedNote);
        RoundRecordedNotePitchToTargetNotePitch(lastRecordedNote);

        // Remember this note
        AddRecordedNote(lastRecordedNote, currentSentence);

        playerController.OnRecordedNoteContinued(lastRecordedNote);
    }

    private void RoundRecordedNotePitchToTargetNotePitch(RecordedNote recordedNote)
    {
        Note targetNote = recordedNote.TargetNote;
        if (targetNote.Type == ENoteType.Rap || targetNote.Type == ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
            recordedNote.RoundedMidiNote = targetNote.MidiNote;
        }
        else
        {
            // Round recorded note if it is close to the target note.
            recordedNote.RoundedMidiNote = GetRoundedMidiNote(recordedNote.RecordedMidiNote, targetNote.MidiNote, RoundingDistance);
        }
    }

    private void LimitRecordedNoteBoundsToTargetNoteBounds(RecordedNote recordedNote)
    {
        Note targetNote = recordedNote.TargetNote;
        if (targetNote != null)
        {
            if (recordedNote.StartBeat < targetNote.StartBeat)
            {
                recordedNote.StartBeat = targetNote.StartBeat;
            }
            if (recordedNote.EndBeat > targetNote.EndBeat)
            {
                recordedNote.EndBeat = targetNote.EndBeat;
            }
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
