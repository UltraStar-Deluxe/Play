using System;
using System.Collections;
using System.Collections.Generic;
using Pitch;
using UnityEngine;

[RequireComponent(typeof(MicrophonePitchTracker))]
public class PlayerNoteRecorder : MonoBehaviour
{
    public List<RecordedNote> recordedNotes = new List<RecordedNote>();

    private SingSceneController singSceneController;

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
    private int lastRecordedFrame;
    private float lastRecordedTime;

    private MicrophonePitchTracker MicrophonePitchTracker
    {
        get
        {
            return GetComponent<MicrophonePitchTracker>();
        }
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
        // Ignore events that happen nearly at same instant (humans won't notice anyway).
        if (lastRecordedTime > Time.time - 0.1f)
        {
            return;
        }
        lastRecordedTime = Time.time;

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
            if (lastRecordedNote == null)
            {
                // Started singing of new note
                lastRecordedNote = new RecordedNote(pitchRecord.MidiNote, currentPositionInMillis, currentPositionInMillis);
                recordedNotes.Add(lastRecordedNote);
                // Debug.Log("started singing");
            }
            else
            {
                if (lastRecordedNote.midiNote == pitchRecord.MidiNote)
                {
                    // Continued singing on same pitch
                    lastRecordedNote.endPositionInMilliseconds = currentPositionInMillis;
                    // Debug.Log("same pitch");
                }
                else
                {
                    // Continued singing on different pitch
                    lastRecordedNote = new RecordedNote(pitchRecord.MidiNote, currentPositionInMillis, currentPositionInMillis);
                    recordedNotes.Add(lastRecordedNote);
                    // Debug.Log("new pitch");
                }
            }
        }
    }
}
