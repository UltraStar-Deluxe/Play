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

    private SentenceDisplayer sentenceDisplayer;

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
        // TODO: This should be instantiated by the singSceneController
        sentenceDisplayer = GameObject.FindObjectOfType<SentenceDisplayer>();

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
                    if (sentenceDisplayer != null)
                    {
                        RecordedSentence recordedSentence = recordedSentences.Where(it => it.Sentence == sentenceDisplayer.CurrentSentence).FirstOrDefault();
                        sentenceDisplayer.DisplayRecordedNotes(recordedSentence);
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
        if (sentenceDisplayer == null)
        {
            return;
        }
        Sentence currentSentence = sentenceDisplayer.CurrentSentence;
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
