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

public class VerticalPitchIndicator : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection
{
    [InjectedInInspector]
    public Image arrowImage;

    [InjectedInInspector]
    public ScrollingNoteStreamDisplayer scrollingNoteStreamDisplayer;

    [Inject]
    private PlayerPitchTracker playerPitchTracker;

    [Inject]
    private MicSampleRecorder micSampleRecorder;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private PlayerNoteRecorder playerNoteRecorder;

    [Inject]
    private SongMeta songMeta;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private Settings settings;

    private RectTransform arrowImageRectTransform;

    private int samplesPerBeat;
    private int lastRecordedRoundedMidiNote;

    void Start()
    {
        if (micProfile == null)
        {
            gameObject.SetActive(false);
            return;
        }

        arrowImageRectTransform = arrowImage.GetComponent<RectTransform>();
        arrowImage.color = micProfile.Color;
        samplesPerBeat = (int)(micSampleRecorder.SampleRateHz * BpmUtils.MillisecondsPerBeat(songMeta) / 1000);
        playerNoteRecorder.RecordedNoteContinuedEventStream
            .Where(recordedNoteEvent => recordedNoteEvent.RecordedNote.TargetNote != null)
            .Subscribe(recordedNoteEvent =>
            {
                lastRecordedRoundedMidiNote = MidiUtils.GetMidiNoteOnOctaveOfTargetMidiNote(
                    recordedNoteEvent.RecordedNote.RoundedMidiNote,
                    recordedNoteEvent.RecordedNote.TargetNote.MidiNote);
            });
        UpdatePosition(60);
    }

    void Update()
    {
        if (!settings.GraphicSettings.showPitchIndicator)
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        if (songAudioPlayer.PositionInSongInMillis <= 0)
        {
            return;
        }

        PitchEvent pitchEvent = playerPitchTracker.GetPitchEventOfSamples(micSampleRecorder.MicSamples.Length - 1 - samplesPerBeat, micSampleRecorder.MicSamples.Length - 1);
        if (pitchEvent != null)
        {
            // Shift midi note to octave of last recorded midi note (can be different because PlayerPitchTracker is rounding towards the target note)
            int midiNote = lastRecordedRoundedMidiNote > 0
                ? MidiUtils.GetRelativePitch(pitchEvent.MidiNote) + (12 * (MidiUtils.GetOctave(lastRecordedRoundedMidiNote) + 1))
                : pitchEvent.MidiNote;
            UpdatePosition(midiNote);
        }
    }

    private void UpdatePosition(int midiNote)
    {
        Vector2 anchorY = scrollingNoteStreamDisplayer.GetAnchorYForMidiNote(midiNote);
        arrowImageRectTransform.anchorMin = new Vector2(arrowImageRectTransform.anchorMin.x, anchorY.x);
        arrowImageRectTransform.anchorMax = new Vector2(arrowImageRectTransform.anchorMax.x, anchorY.y);
        arrowImageRectTransform.MoveCornersToAnchors();
    }
}
