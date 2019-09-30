using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pitch;
using UnityEngine;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour
{
    public List<RecordedNote> recordedNotes = new List<RecordedNote>();
    public List<RecordedSentence> recordedSentences = new List<RecordedSentence>();

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

    private RecordedNote lastRecordedNote;
    private RecordedSentence lastRecordedSentence;
    private int lastRecordedFrame;

    private MicrophonePitchTracker MicrophonePitchTracker
    {
        get
        {
            return GetComponent<MicrophonePitchTracker>();
        }
    }

    public void Init(PlayerController playerController)
    {
        this.playerController = playerController;
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
                // Debug.Log("ended singing");
                lastRecordedNote = null;
            }
        }
        else
        {
            double currentPositionInMillis = singSceneController.PositionInSongInMillis;
            double currentBeat = singSceneController.CurrentBeat;
            if (lastRecordedNote == null)
            {
                // Started singing of new note
                lastRecordedNote = new RecordedNote(pitchRecord.MidiNote, currentPositionInMillis, currentPositionInMillis, currentBeat, currentBeat);
                AddRecordedNote(lastRecordedNote);
                // Debug.Log("started singing");
            }
            else
            {
                if (lastRecordedNote.MidiNote == pitchRecord.MidiNote)
                {
                    // Continued singing on same pitch
                    lastRecordedNote.EndPositionInMilliseconds = currentPositionInMillis;
                    lastRecordedNote.EndBeat = currentBeat;
                    // Debug.Log("same pitch, pos in millis " + currentPositionInMillis);
                    if (playerController != null)
                    {
                        RecordedSentence recordedSentence = recordedSentences.Where(it => it.Sentence == playerController.CurrentSentence).FirstOrDefault();
                        playerController.DisplayRecordedSentence(recordedSentence);
                    }
                }
                else
                {
                    // Continued singing on different pitch
                    lastRecordedNote = new RecordedNote(pitchRecord.MidiNote, currentPositionInMillis, currentPositionInMillis, currentBeat, currentBeat);
                    AddRecordedNote(lastRecordedNote);
                    // Debug.Log("new pitch");
                }
            }
        }
    }

    private void AddRecordedNote(RecordedNote recordedNote)
    {
        recordedNotes.Add(lastRecordedNote);

        // Find corresponding sentence for recorded note
        if (playerController == null)
        {
            return;
        }
        Sentence currentSentence = playerController.CurrentSentence;
        if (currentSentence == null)
        {
            return;
        }

        // Create RecordedSentence for the currently displayed Sentence.
        if (lastRecordedSentence == null || lastRecordedSentence.Sentence != currentSentence)
        {
            lastRecordedSentence = new RecordedSentence(currentSentence);
            recordedSentences.Add(lastRecordedSentence);
            // Debug.Log("new rec sentence");
        }

        // Add note to RecordedSentence if it fits.
        if (recordedNote.StartBeat < lastRecordedSentence.Sentence.EndBeat &&
            recordedNote.EndBeat > lastRecordedSentence.Sentence.StartBeat)
        {
            lastRecordedSentence.AddRecordedNote(recordedNote);
            // Debug.Log("add note to rec sentence");
        }
    }
}
