using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class SongEditorMidiSoundPlayAlongThread
{
    private Settings settings;
    private SongMeta songMeta;
    private MidiManager midiManager;
    private ThreadPool.PoolHandle poolHandle;

    private bool isStopped;

    private int positionInSongInMillis;
    private List<Note> upcomingSortedNotes = new List<Note>();

    private DateTime playbackStartDateTime = DateTime.Now;
    private DateTime awaitedNoteStartDateTime = DateTime.Now;
    private DateTime awaitedNoteEndDateTime = DateTime.Now;

    private int lastPlayedMidiNote = -1;

    public SongEditorMidiSoundPlayAlongThread(Settings settings, SongMeta songMeta, MidiManager midiManager)
    {
        this.settings = settings;
        this.songMeta = songMeta;
        this.midiManager = midiManager;
    }

    public void Start(int positionInSongInMillis)
    {
        if (poolHandle != null)
        {
            throw new SongEditorMidiSoundPlayAlongException("Attempt to start midi-play-along-thread again");
        }
        if (isStopped)
        {
            throw new SongEditorMidiSoundPlayAlongException("Attempt to restart midi-play-along-thread. Create a new instance instead.");
        }

        SynchronizeWithPlaybackPosition(positionInSongInMillis);
        CalculateUpcomingSortedNotes();
        poolHandle = ThreadPool.QueueUserWorkItem((handle) => Run());
    }

    public void Stop()
    {
        isStopped = true;
    }

    private void Run()
    {
        while (!isStopped
                && !upcomingSortedNotes.IsNullOrEmpty())
        {
            UpdateMidiNotePlayback();
        }
    }

    public void CalculateUpcomingSortedNotes()
    {
        upcomingSortedNotes = GetUpcomingSortedNotes(positionInSongInMillis);
    }

    private void UpdateMidiNotePlayback()
    {
        // Assumption: There is not more than one note at a time.
        // Calculate where time currently is now: inside the note or not.
        // ------|Note1Start------NOW----Note1End|-----------|Note2Start------Note2End|----------

        Note awaitedNote = upcomingSortedNotes[0];

        // Check if note should be played
        float playbackSpeedFactor = 1 / settings.SongEditorSettings.MusicPlaybackSpeed;
        int awaitedNoteStartInMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, awaitedNote.StartBeat);
        awaitedNoteStartDateTime = playbackStartDateTime.AddMilliseconds(awaitedNoteStartInMillis * playbackSpeedFactor);
        DateTime now = DateTime.Now - new TimeSpan(0, 0, 0, 0, settings.SongEditorSettings.MidiPlaybackOffsetInMillis);

        if (awaitedNoteStartDateTime < now)
        {
            int awaitedNoteEndInMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, awaitedNote.EndBeat);
            awaitedNoteEndDateTime = playbackStartDateTime.AddMilliseconds(awaitedNoteEndInMillis * playbackSpeedFactor);
            if (now < awaitedNoteEndDateTime)
            {
                // NOW is "inside" the note. Thus, play it.
                PlayMidiNote(awaitedNote);

                // Sleep until end of note is reached.
                TimeSpan timeSpanUntilNoteEnds = awaitedNoteEndDateTime - now;
                Thread.Sleep(timeSpanUntilNoteEnds);

                StopMidiNote(awaitedNote);
            }
            else
            {
                // NOW is already after the end of the note. Thus, stop all sounds and await next note to come (in next iteration of while-loop).
                midiManager.StopAllMidiNotes();
            }

            // This note is done.
            upcomingSortedNotes.RemoveAt(0);
        }
        else
        {
            // NOW is not yet where the note starts. Thus, sleep until it starts.
            // The note will be tested again in the next iteration.
            TimeSpan timeSpanUntilNoteStarts = awaitedNoteStartDateTime - now;
            Thread.Sleep(timeSpanUntilNoteStarts);
        }
    }

    private TimeSpan ScaleTimeSpan(TimeSpan timeSpan, float factor)
    {
        int days = (int)(timeSpan.Days * factor);
        int hours = (int)(timeSpan.Hours * factor);
        int minutes = (int)(timeSpan.Minutes * factor);
        int seconds = (int)(timeSpan.Seconds * factor);
        int millis = (int)(timeSpan.Milliseconds * factor);
        return new TimeSpan(days, hours, minutes, seconds, millis);
    }

    private void PlayMidiNote(Note awaitedNote)
    {
        if (lastPlayedMidiNote >= 0)
        {
            midiManager.StopMidiNote(lastPlayedMidiNote);
        }
        midiManager.PlayMidiNote(awaitedNote.MidiNote);
        lastPlayedMidiNote = awaitedNote.MidiNote;
    }

    private void StopMidiNote(Note awaitedNote)
    {
        midiManager.StopMidiNote(awaitedNote.MidiNote);
    }

    public void SynchronizeWithPlaybackPosition(int positionInSongInMillis)
    {
        float playbackSpeedFactor = 1 / settings.SongEditorSettings.MusicPlaybackSpeed;
        playbackStartDateTime = DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 0, (int)(positionInSongInMillis * playbackSpeedFactor)));
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

    public class SongEditorMidiSoundPlayAlongException : Exception
    {
        public SongEditorMidiSoundPlayAlongException(string message) : base(message)
        {
        }
    }
}
