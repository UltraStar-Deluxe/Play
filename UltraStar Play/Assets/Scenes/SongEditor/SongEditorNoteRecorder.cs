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
    public int octaveOffset;
    public int delayInMillis;

    public int midiNoteForButtonRecording;

    [Inject]
    private MicrophonePitchTracker microphonePitchTracker;

    [Inject]
    private Settings settings;

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
        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.RecordingSource)
            .Subscribe(OnNoteRecordingSourceChanged);
        songAudioPlayer.ObserveEveryValueChanged(it => it.IsPlaying)
            .Subscribe(OnSongIsPlayingChanged);

        microphonePitchTracker.PitchEventStream
            .Subscribe(pitchEvent => OnPitchDetected(pitchEvent, ESongEditorLayer.MicRecording));
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
        bool keyboardButtonRecordingEnabled = (settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.KeyboardButton);
        if (keyboardButtonRecordingEnabled && Input.GetKey(KeyCode.F8))
        {
            OnPitchDetected(new PitchEvent(midiNoteForButtonRecording), ESongEditorLayer.ButtonRecording);
        }
    }

    private void OnPitchDetected(PitchEvent pitchEvent, ESongEditorLayer targetLayer)
    {
        if (pitchEvent == null || lastPitchDetectedFrame == Time.frameCount)
        {
            return;
        }

        double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis - delayInMillis;
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);

        int midiNote = pitchEvent.MidiNote + (octaveOffset * 12);
        if (lastRecordedNote != null
            && lastRecordedNote.MidiNote == midiNote
            && lastPitchDetectedFrame == Time.frameCount - 1)
        {
            ContinueLastRecordedNote(midiNote, currentBeat);
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

    private void ContinueLastRecordedNote(int midiNote, int currentBeat)
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
