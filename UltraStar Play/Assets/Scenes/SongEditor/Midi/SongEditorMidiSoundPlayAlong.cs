using System.Collections.Generic;
using System.Linq;
using CSharpSynth.Midi;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorMidiSoundPlayAlong : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    private bool isPlaying;
    private float startTimeInSeconds;
    
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
            .Where(note => note.StartBeat > currentPositionInBeats)
            .ToList();
        if (followingNotes.IsNullOrEmpty())
        {
            return;
        }
        
        followingNotes.Sort(Note.comparerByStartBeat);
        Note firstNote = followingNotes.FirstOrDefault();
        double firstNoteStartInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, firstNote.StartBeat);
        double distanceToFirstNoteStartInMillis = firstNoteStartInMillis - songAudioPlayer.PositionInSongInMillis;
        distanceToFirstNoteStartInMillis += settings.SongEditorSettings.MidiPlaybackOffsetInMillis;
        
        Debug.Log($"distanceToFirstNoteStartInMillis before {distanceToFirstNoteStartInMillis}");
        if (distanceToFirstNoteStartInMillis < 0)
        {
            distanceToFirstNoteStartInMillis = 0;
        }
        Debug.Log($"distanceToFirstNoteStartInMillis after {distanceToFirstNoteStartInMillis}");
        
        MidiFile midiFile = MidiFileUtils.CreateMidiFile(
            songMeta,
            followingNotes,
            (byte)settings.SongEditorSettings.MidiVelocity);
        MidiFileUtils.SetFirstDeltaTimeTo(midiFile, (uint)distanceToFirstNoteStartInMillis);
        midiManager.PlayMidiFile(midiFile);
        
        isPlaying = true;
        startTimeInSeconds = Time.time;
    }
}
