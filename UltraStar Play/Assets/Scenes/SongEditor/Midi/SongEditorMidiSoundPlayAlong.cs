using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using CSharpSynth.Midi;

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

    private readonly HashSet<Note> currentlyPlayingNotes = new HashSet<Note>();

    private bool songAudioPlayerIsPlayingOld;
    private double positionInSongInMillisOld;

    private bool hasSearchedUpcomingSortedNotes;
    private List<Note> upcomingSortedNotes = new List<Note>();

    void Update()
    {
        if (!settings.SongEditorSettings.MidiSoundPlayAlongEnabled)
        {
            // Do not play midi sounds.
            // Furthermore, stop currently playing sounds if this setting changed during playback.
            if (!currentlyPlayingNotes.IsNullOrEmpty())
            {
                StopAllMidiSounds();
                hasSearchedUpcomingSortedNotes = false;
            }
            return;
        }

        if (songAudioPlayer.IsPlaying)
        {
            double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;

            SynchMidiSoundWithPlayback(positionInSongInMillis);

            // Remember old playback position
            positionInSongInMillisOld = positionInSongInMillis;
        }
        else if (songAudioPlayerIsPlayingOld)
        {
            // Stopped playing, thus stop midi sounds.
            StopAllMidiSounds();
            hasSearchedUpcomingSortedNotes = false;
        }
        songAudioPlayerIsPlayingOld = songAudioPlayer.IsPlaying;
    }

    private void SynchMidiSoundWithPlayback(double positionInSongInMillis)
    {
        if (!hasSearchedUpcomingSortedNotes)
        {
            upcomingSortedNotes = GetUpcomingSortedNotes(positionInSongInMillis);
            positionInSongInMillisOld = positionInSongInMillis;
            hasSearchedUpcomingSortedNotes = true;
        }

        if (positionInSongInMillis < positionInSongInMillisOld)
        {
            // Jumped back, thus recalculate upcomingSortedNotes and stop any currently playing notes.
            upcomingSortedNotes = GetUpcomingSortedNotes(positionInSongInMillis);
            StopAllMidiSounds();
        }

        // Play newly entered notes
        StartMidiSoundForEnteredNotes(positionInSongInMillis);

        // Stop notes that have been left
        StopMidiSoundForLeftNotes(positionInSongInMillis);
    }

    private void StopMidiSoundForLeftNotes(double positionInSongInMillis)
    {
        List<Note> newlyLeftNotes = new List<Note>();
        foreach (Note note in currentlyPlayingNotes)
        {
            double endMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat);

            if (endMillis < positionInSongInMillis)
            {
                newlyLeftNotes.Add(note);
                midiManager.StopMidiNote(note.MidiNote);
            }
        }
        newlyLeftNotes.ForEach(it => currentlyPlayingNotes.Remove(it));
    }

    private void StartMidiSoundForEnteredNotes(double positionInSongInMillis)
    {
        int newlyEnteredNoteCount = 0;
        foreach (Note note in upcomingSortedNotes)
        {
            double startMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);

            if (positionInSongInMillis < startMillis)
            {
                // The list is sorted, thus we did not reach any of the following notes in the list as well.
                break;
            }
            else
            {
                newlyEnteredNoteCount++;
                midiManager.PlayMidiNote(note.MidiNote);
                currentlyPlayingNotes.Add(note);
            }
        }
        if (newlyEnteredNoteCount > 0)
        {
            upcomingSortedNotes.RemoveRange(0, newlyEnteredNoteCount);
        }
    }

    // Compute the upcoming notes, i.e., the notes that have not yet been finished at the playback position.
    private List<Note> GetUpcomingSortedNotes(double positionInSongInMillis)
    {
        List<Note> result = SongMetaUtils.GetAllNotes(songMeta)
            .Where(note => BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat) > positionInSongInMillis)
            .ToList();
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    private void StopAllMidiSounds()
    {
        midiManager.StopAllMidiNotes();
        currentlyPlayingNotes.Clear();
    }
}
