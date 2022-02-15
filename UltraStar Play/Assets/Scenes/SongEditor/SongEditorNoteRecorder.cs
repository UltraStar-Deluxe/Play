using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorNoteRecorder : MonoBehaviour, INeedInjection
{
    [Inject]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private Settings settings;

    private SongEditorSettings EditorSettings
    {
        get
        {
            return settings.SongEditorSettings;
        }
    }

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongEditorHistoryManager historyManager;

    private List<Note> upcomingSortedRecordedNotes = new List<Note>();

    private int lastPitchDetectedFrame;
    private int lastPitchDetectedBeat;
    private Note lastRecordedNote;

    private bool hasRecordedNotes;

    private bool isRecording;

    public bool IsRecordingEnabled { get; set; }

    void Start()
    {
        micPitchTracker.MicProfile = settings.MicProfiles.FirstOrDefault(it => it.IsEnabled && it.IsConnected);

        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.RecordingSource).Subscribe(OnNoteRecordingSourceChanged);
        songAudioPlayer.ObserveEveryValueChanged(it => it.IsPlaying).Subscribe(OnSongIsPlayingChanged);
        songAudioPlayer.JumpBackInSongEventStream.Subscribe(OnJumpedBackInSong);
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(OnPlaybackStarted);
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(OnPlaybackStopped);

        micPitchTracker.PitchEventStream.Subscribe(pitchEvent => OnPitchDetected(pitchEvent));
        if (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.Microphone)
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }
    }

    private void OnPlaybackStopped(double positionInSongInMillis)
    {
        if (hasRecordedNotes)
        {
            historyManager.AddUndoState();
        }
    }

    private void OnPlaybackStarted(double positionInSongInMillis)
    {
        hasRecordedNotes = false;
        lastPitchDetectedBeat = GetCurrentBeat(positionInSongInMillis);
        upcomingSortedRecordedNotes = GetUpcomingSortedRecordedNotes();
    }

    void Update()
    {
        if (songAudioPlayer.IsPlaying)
        {
            UpdateRecordingViaButtonClick();
        }
    }

    private void OnJumpedBackInSong(Pair<double> previousAndNewPositionInMillis)
    {
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, previousAndNewPositionInMillis.Current);
        lastPitchDetectedBeat = currentBeat;
        upcomingSortedRecordedNotes = GetUpcomingSortedRecordedNotes();
    }

    private void UpdateRecordingViaButtonClick()
    {
        if (!IsRecordingEnabled)
        {
            return;
        }

        // Record notes via button click.
        bool keyboardButtonRecordingEnabled = (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.KeyboardButton_F8);
        if (keyboardButtonRecordingEnabled)
        {
            double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
            if (Keyboard.current != null && Keyboard.current.f8Key.isPressed)
            {
                RecordNote(EditorSettings.MidiNoteForButtonRecording, positionInSongInMillis, ESongEditorLayer.ButtonRecording);
            }
            else
            {
                lastRecordedNote = null;
                // The pitch is always detected (either the keyboard is down or not).
                lastPitchDetectedBeat = GetCurrentBeat(positionInSongInMillis);
            }
        }
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (!IsRecordingEnabled)
        {
            return;
        }

        if (!isRecording
            || lastPitchDetectedFrame == Time.frameCount)
        {
            return;
        }

        if (pitchEvent == null)
        {
            lastRecordedNote = null;
            return;
        }

        double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis - EditorSettings.MicDelayInMillis;
        int midiNote = pitchEvent.MidiNote + (EditorSettings.MicOctaveOffset * 12);
        RecordNote(midiNote, positionInSongInMillis, ESongEditorLayer.MicRecording);
    }

    private void RecordNote(int midiNote, double positionInSongInMillis, ESongEditorLayer targetLayer)
    {
        int currentBeat = GetCurrentBeat(positionInSongInMillis);
        if (currentBeat <= lastPitchDetectedBeat)
        {
            return;
        }

        if (lastRecordedNote != null
            && lastRecordedNote.MidiNote == midiNote)
        {
            // Also fire the event for any skipped beats (e.g. because of low frame-rate)
            for (int beat = lastPitchDetectedBeat; beat <= currentBeat; beat++)
            {
                ContinueLastRecordedNote(beat, targetLayer);
            }
        }
        else
        {
            CreateNewRecordedNote(midiNote, currentBeat, targetLayer);
        }

        editorNoteDisplayer.UpdateNotes();

        lastPitchDetectedFrame = Time.frameCount;
        lastPitchDetectedBeat = currentBeat;
        hasRecordedNotes = true;
    }

    private void CreateNewRecordedNote(int midiNote, int currentBeat, ESongEditorLayer targetLayer)
    {
        lastRecordedNote = new Note(ENoteType.Normal, currentBeat, 1, midiNote - 60, "");
        songEditorLayerManager.AddNoteToLayer(targetLayer, lastRecordedNote);

        // EndBeat of new note is currentBeat + 1. Overwrite notes that start before this beat.
        OverwriteExistingNotes(currentBeat + 1, targetLayer);
    }

    private void ContinueLastRecordedNote(int currentBeat, ESongEditorLayer targetLayer)
    {
        if (currentBeat > lastRecordedNote.EndBeat)
        {
            lastRecordedNote.SetEndBeat(currentBeat);

            // EndBeat of extended note is currentBeat. Overwrite notes that start before this beat.
            OverwriteExistingNotes(currentBeat, targetLayer);
        }
    }

    private void OverwriteExistingNotes(int currentBeat, ESongEditorLayer targetLayer)
    {
        // Move the start beat of existing notes behind the given beat.
        // If afterwards no length would be left (or negative), then remove the note completely.
        List<Note> overlappingNotes = new List<Note>();
        int behindNoteCount = 0;
        foreach (Note upcomingNote in upcomingSortedRecordedNotes)
        {
            // Do not shorten the note that is currently beeing recorded.
            if (upcomingNote == lastRecordedNote)
            {
                continue;
            }

            if (upcomingNote.StartBeat < currentBeat && currentBeat <= upcomingNote.EndBeat)
            {
                overlappingNotes.Add(upcomingNote);
            }
            else if (upcomingNote.EndBeat < currentBeat)
            {
                // The position is behind the note, thus this note is not 'upcoming' anymore.
                behindNoteCount++;
            }
            else if (upcomingNote.EndBeat > currentBeat)
            {
                // The list is sorted, thus the other notes in the list will also not overlap with the currentBeat.
                break;
            }
        }
        if (behindNoteCount > 0)
        {
            upcomingSortedRecordedNotes.RemoveRange(0, behindNoteCount);
        }

        foreach (Note note in overlappingNotes)
        {
            if (note.EndBeat > currentBeat)
            {
                note.SetStartBeat(currentBeat);
            }
            else
            {
                songEditorLayerManager.RemoveNoteFromAllLayers(note);
                editorNoteDisplayer.DeleteNoteControl(note);
            }
        }
    }

    private void OnNoteRecordingSourceChanged(ESongEditorRecordingSource recordingSource)
    {
        bool micRecordingEnabled = (recordingSource == ESongEditorRecordingSource.Microphone);
        if (!micRecordingEnabled && isRecording)
        {
            StopRecording();
        }
        else if (micRecordingEnabled && songAudioPlayer.IsPlaying && !isRecording)
        {
            StartRecording();
        }

        if (micRecordingEnabled)
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }
        else
        {
            micPitchTracker.MicSampleRecorder.StopRecording();
        }
    }

    private void OnSongIsPlayingChanged(bool isPlaying)
    {
        bool micRecordingEnabled = (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.Microphone);
        if (!isPlaying && isRecording)
        {
            StopRecording();
        }
        else if (isPlaying && !isRecording
                 && micRecordingEnabled && micPitchTracker.MicProfile != null)
        {
            StartRecording();
        }
    }

    private List<Note> GetUpcomingSortedRecordedNotes()
    {
        int currentBeat = GetCurrentBeat(songAudioPlayer.PositionInSongInMillis - settings.SongEditorSettings.MicDelayInMillis);
        ESongEditorLayer targetLayer = GetRecordingTargetLayer();
        List<Note> result = songEditorLayerManager.GetNotes(targetLayer).Where(note => (note.StartBeat >= currentBeat)).ToList();
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    private ESongEditorLayer GetRecordingTargetLayer()
    {
        switch (settings.SongEditorSettings.RecordingSource)
        {
            case ESongEditorRecordingSource.KeyboardButton_F8:
                return ESongEditorLayer.ButtonRecording;
            case ESongEditorRecordingSource.Microphone:
                return ESongEditorLayer.MicRecording;
            default:
                return ESongEditorLayer.ButtonRecording;
        }
    }

    private int GetCurrentBeat(double positionInSongInMillis)
    {
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
        return currentBeat;
    }

    private void StopRecording()
    {
        isRecording = false;
    }

    private void StartRecording()
    {
        isRecording = true;
    }
}
