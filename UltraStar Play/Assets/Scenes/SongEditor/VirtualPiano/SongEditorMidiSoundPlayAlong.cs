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

    private List<Note> upcomingSortedNotes = null;
    private HashSet<Note> currentlyPlayingNotes = new HashSet<Note>();

    private bool songAudioPlayerIsPlayingOld;
    private double positionInSongInMillisOld;

    void Update()
    {
        if (!settings.SongEditorSettings.MidiSoundPlayAlongEnabled)
        {
            if (!currentlyPlayingNotes.IsNullOrEmpty())
            {
                StopAllMidiSounds();
                upcomingSortedNotes = null;
            }
            return;
        }

        if (songAudioPlayer.IsPlaying)
        {
            double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
            if (upcomingSortedNotes == null)
            {
                // Compute the upcoming notes, i.e., the notes starting after the current playback position.
                upcomingSortedNotes = GetUpcomingSortedNotes();
                positionInSongInMillisOld = positionInSongInMillis;
            }

            if (positionInSongInMillis < positionInSongInMillisOld)
            {
                // Jumped back, thus recalculate upcomingSortedNotes and stop any currently playing notes.
                upcomingSortedNotes = GetUpcomingSortedNotes();
                StopAllMidiSounds();
            }

            // Play the upcoming notes
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

            // Stop notes that have been left
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

            // Remember old playback position
            positionInSongInMillisOld = positionInSongInMillis;
        }
        else if (songAudioPlayerIsPlayingOld)
        {
            // Stopped playing, thus stop midi sounds.
            StopAllMidiSounds();
            upcomingSortedNotes = null;
        }
        songAudioPlayerIsPlayingOld = songAudioPlayer.IsPlaying;
    }

    private List<Note> GetUpcomingSortedNotes()
    {
        double positionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        List<Note> result = SongMetaUtils.GetAllNotes(songMeta)
            .Where(note => BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat) >= positionInSongInMillis)
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
