using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniInject;

public class SongEditorMidiSoundPlayAlongThread : INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private SongEditorLayerManager layerManager;

    private ThreadPool.PoolHandle poolHandle;

    private bool isStopped;

    private List<Note> upcomingSortedNotes = new();

    private DateTime playbackStartDateTime = DateTime.Now;

    private int lastPlayedMidiNote = -1;

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
        CalculateUpcomingSortedNotes(positionInSongInMillis);
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

    public void CalculateUpcomingSortedNotes(int positionInSongInMillis)
    {
        upcomingSortedNotes = GetUpcomingSortedNotes(positionInSongInMillis);
    }

    private void UpdateMidiNotePlayback()
    {
        Note awaitedNote = upcomingSortedNotes[0];

        // Calculate where time currently is now: inside the note or not.
        float playbackSpeedFactor = 1 / settings.SongEditorSettings.MusicPlaybackSpeed;
        int awaitedNoteStartInMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, awaitedNote.StartBeat);
        DateTime awaitedNoteStartDateTime = playbackStartDateTime.AddMilliseconds(awaitedNoteStartInMillis * playbackSpeedFactor);
        DateTime now = DateTime.Now - new TimeSpan(0, 0, 0, 0, settings.SongEditorSettings.MidiPlaybackOffsetInMillis);

        if (awaitedNoteStartDateTime < now)
        {
            int awaitedNoteEndInMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, awaitedNote.EndBeat);
            DateTime awaitedNoteEndDateTime = playbackStartDateTime.AddMilliseconds(awaitedNoteEndInMillis * playbackSpeedFactor);
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

    // Compute the upcoming notes, i.e., the visible notes that have not yet been finished at the playback position.
    private List<Note> GetUpcomingSortedNotes(double positionInSongInMillis)
    {
        List<Note> notesInSongMeta = songMeta.GetVoices()
            .Where(voice => layerManager.IsVoiceVisible(voice))
            .SelectMany(voice => voice.Sentences)
            .SelectMany(sentence => sentence.Notes)
            .ToList();
        List<Note> allNotes = notesInSongMeta
            .Union(layerManager.GetAllVisibleNotes())
            .ToList();
        List<Note> allUpcomingNotes = allNotes
            .Where(note => !note.IsFreestyle && !note.IsRap)
            .Where(note => BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat) > positionInSongInMillis)
            .ToList();
        allUpcomingNotes.Sort(Note.comparerByStartBeat);
        return allUpcomingNotes;
    }

    public class SongEditorMidiSoundPlayAlongException : Exception
    {
        public SongEditorMidiSoundPlayAlongException(string message) : base(message)
        {
        }
    }
}
