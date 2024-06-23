using System.Collections.Generic;
using System.Linq;
using AudioSynthesis.Midi;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMidiSoundPlayAlong : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    private bool isPlaying;
    private float startTimeInSeconds;

    private void Start()
    {
        songAudioPlayer.JumpBackEventStream.Subscribe(_ => RestartMidiPlayAlong());
        songAudioPlayer.JumpForwardEventStream.Subscribe(_ => RestartMidiPlayAlong());
    }

    void Update()
    {
        if (!settings.SongEditorSettings.MidiSoundPlayAlongEnabled)
        {
            if (isPlaying)
            {
                StopMidiPlayAlong();
            }
            return;
        }

        if (songAudioPlayer.IsPlaying
            && !isPlaying)
        {
            StartMidiPlayAlong();
        }
        else if (!songAudioPlayer.IsPlaying
                 && isPlaying)
        {
            StopMidiPlayAlong();
        }
    }

    private void RestartMidiPlayAlong()
    {
        if (!isPlaying)
        {
            return;
        }
        StopMidiPlayAlong();
        StartMidiPlayAlong();
    }

    private bool InsideAnyVisibleNote()
    {
        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);
        foreach (Note note in songEditorSceneControl.GetAllVisibleNotes())
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                if (SongMetaUtils.IsBeatInNote(note, currentBeat + offset))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void StopMidiPlayAlong()
    {
        midiManager.StopMidiFile();
        isPlaying = false;
    }

    private void StartMidiPlayAlong()
    {
        StopMidiPlayAlong();

        double currentPositionInBeats = songAudioPlayer.GetCurrentBeat(true);
        List<Note> allVisibleNotes = songEditorSceneControl.GetAllVisibleNotes();
        List<Note> followingNotes = allVisibleNotes
            .Where(note => note.StartBeat > currentPositionInBeats
                           && layerManager.IsMidiSoundPlayAlongEnabled(layerManager.GetLayer(note)))
            .ToList();
        if (followingNotes.IsNullOrEmpty())
        {
            return;
        }

        followingNotes.Sort(Note.comparerByStartBeat);
        Note firstNote = followingNotes.FirstOrDefault();
        double firstNoteStartInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, firstNote.StartBeat);
        double distanceToFirstNoteStartInMillis = firstNoteStartInMillis - songAudioPlayer.PositionInMillis;
        distanceToFirstNoteStartInMillis += settings.SongEditorSettings.MidiPlaybackOffsetInMillis;

        if (distanceToFirstNoteStartInMillis < 0)
        {
            distanceToFirstNoteStartInMillis = 0;
        }

        float timeFactor = nonPersistentSettings.SongEditorMusicPlaybackSpeed.Value > 0
            ? 1 / nonPersistentSettings.SongEditorMusicPlaybackSpeed.Value
            : 1;
        MidiFile midiFile = MidiFileUtils.CreateMidiFile(
            songMeta,
            followingNotes,
            (byte)settings.SongEditorSettings.MidiVelocity,
            0,
            timeFactor);
        MidiFileUtils.SetFirstDeltaTimeTo(midiFile, 0, (int)(distanceToFirstNoteStartInMillis * timeFactor));
        midiManager.PlayMidiFile(midiFile);

        isPlaying = true;
        startTimeInSeconds = Time.time;
    }
}
