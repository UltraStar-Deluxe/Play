using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorNoteRecorder : MonoBehaviour, INeedInjection
{
    [Inject]
    private MicrophonePitchTracker microphonePitchTracker;

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

    private int lastPitchDetectedFrame;

    private Note lastRecordedNote;

    void Start()
    {
        microphonePitchTracker.MicProfile = settings.MicProfiles.Where(it => it.IsEnabled && it.IsConnected).FirstOrDefault();

        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.RecordingSource)
            .Subscribe(OnNoteRecordingSourceChanged);
        songAudioPlayer.ObserveEveryValueChanged(it => it.IsPlaying)
            .Subscribe(OnSongIsPlayingChanged);

        microphonePitchTracker.PitchEventStream
            .Subscribe(pitchEvent => OnPitchDetected(pitchEvent));
    }

    void Update()
    {
        if (songAudioPlayer.IsPlaying)
        {
            UpdateRecordingViaButtonClick();
        }
    }

    private void UpdateRecordingViaButtonClick()
    {
        // Record notes via button click.
        bool keyboardButtonRecordingEnabled = (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.KeyboardButton_F8);
        if (keyboardButtonRecordingEnabled && Input.GetKey(KeyCode.F8))
        {
            RecordNote(EditorSettings.MidiNoteForButtonRecording, songAudioPlayer.PositionInSongInMillis, ESongEditorLayer.ButtonRecording);
        }
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (pitchEvent == null || lastPitchDetectedFrame == Time.frameCount)
        {
            return;
        }

        double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis - EditorSettings.MicDelayInMillis;
        int midiNote = pitchEvent.MidiNote + (EditorSettings.MicOctaveOffset * 12);
        RecordNote(midiNote, positionInSongInMillis, ESongEditorLayer.MicRecording);
    }

    private void RecordNote(int midiNote, double positionInSongInMillis, ESongEditorLayer targetLayer)
    {
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);

        if (lastRecordedNote != null
            && lastRecordedNote.MidiNote == midiNote
            && lastPitchDetectedFrame == Time.frameCount - 1)
        {
            ContinueLastRecordedNote(currentBeat);
        }
        else
        {
            CreateNewRecordedNote(midiNote, currentBeat, targetLayer);
        }

        lastPitchDetectedFrame = Time.frameCount;
    }

    private void CreateNewRecordedNote(int midiNote, int currentBeat, ESongEditorLayer targetLayer)
    {
        lastRecordedNote = new Note(ENoteType.Normal, currentBeat, 1, midiNote - 60, " ");
        songEditorLayerManager.AddNoteToLayer(targetLayer, lastRecordedNote);
    }

    private void ContinueLastRecordedNote(int currentBeat)
    {
        if (currentBeat > lastRecordedNote.EndBeat)
        {
            lastRecordedNote.SetEndBeat(currentBeat);
        }
    }

    private void OnNoteRecordingSourceChanged(ESongEditorRecordingSource recordingSource)
    {
        bool micRecordingEnabled = (recordingSource == ESongEditorRecordingSource.Microphone);
        if (!micRecordingEnabled && microphonePitchTracker.IsPitchDetectionRunning())
        {
            microphonePitchTracker.StopPitchDetection();
        }
        else if (micRecordingEnabled && songAudioPlayer.IsPlaying && !microphonePitchTracker.IsPitchDetectionRunning())
        {
            microphonePitchTracker.StartPitchDetection();
        }
    }

    private void OnSongIsPlayingChanged(bool isPlaying)
    {
        bool micRecordingEnabled = (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.Microphone);
        if (!isPlaying && microphonePitchTracker.IsPitchDetectionRunning())
        {
            microphonePitchTracker.StopPitchDetection();
        }
        else if (isPlaying && micRecordingEnabled && !microphonePitchTracker.IsPitchDetectionRunning())
        {
            microphonePitchTracker.StartPitchDetection();
        }
    }
}
