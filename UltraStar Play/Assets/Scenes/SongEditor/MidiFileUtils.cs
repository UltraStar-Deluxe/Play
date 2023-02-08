using System;
using System.Collections.Generic;
using System.Linq;
using CSharpSynth.Midi;

public static class MidiFileUtils
{
    public static List<int> GetTrackIndexes(MidiFile midiFile)
    {
        if (midiFile == null)
        {
            return new();
        }
        return NumberUtils.CreateIntList(0, midiFile.Tracks.Length - 1);
    }
    
    public static List<int> GetChannelIndexes(MidiTrack track, bool onlyWithNotes)
    {
        if (track == null)
        {
            return new();
        }

        List<MidiEvent> midiEvents = track.MidiEvents.ToList();
        if (onlyWithNotes)
        {
            midiEvents = midiEvents
                .Where(midiEvent => midiEvent.midiChannelEvent is MidiHelper.MidiChannelEvent.Note_On)
                .ToList();
        }
        return midiEvents.Select(midiEvent => (int)midiEvent.channel)
            .Distinct()
            .OrderBy(channelIndex => channelIndex)
            .ToList();
    }

    public static List<MidiEvent> GetLyricsEvents(MidiTrack track)
    {
        List<MidiEvent> lyricsEvents = track.MidiEvents
            .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Lyric_Text)
            .ToList();
        List<MidiEvent> textEvent = track.MidiEvents
            .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Text_Event)
            .ToList();
        List<MidiEvent> markerTextEvents = track.MidiEvents
            .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Marker_Text)
            .ToList();
        List<MidiEvent> actualLyricsEvents = new List<List<MidiEvent>> { lyricsEvents, textEvent, markerTextEvents }
            .FindMaxElement(events => events.Count);

        return actualLyricsEvents;
    }

    public static string GetLyrics(MidiEvent midiEvent)
    {
        if (midiEvent.Parameters.IsNullOrEmpty()
            || midiEvent.Parameters[0] is not string)
        {
            return null;
        }

        string rawLyrics = midiEvent.Parameters[0] as string;
        return rawLyrics.Replace("\r", "\n");   
    }
    
    public static string GetLyrics(MidiTrack track)
    {
        List<MidiEvent> lyricsEvents = GetLyricsEvents(track);
        return lyricsEvents
            .Select(midiEvent => GetLyrics(midiEvent))
            .JoinWith("");
    }
    
    public static int GetDeltaTimeInMillis(MidiEvent midiEvent)
    {
        uint deltaTimeInSamples = midiEvent.deltaTime;
        int deltaTimeInMillis = (int)Math.Round(deltaTimeInSamples / (MidiManager.midiStreamSampleRateHz / 1000.0));
        return deltaTimeInMillis;
    }
    
    public static void SetFirstDeltaTimeTo(MidiFile midiFile, uint value)
    {
        midiFile.Tracks.ForEach(track =>
        {
            MidiEvent firstNoteOnEvent = track.MidiEvents.FirstOrDefault(midiEvent => midiEvent.midiChannelEvent == MidiHelper.MidiChannelEvent.Note_On);
            if (firstNoteOnEvent != null)
            {
                firstNoteOnEvent.deltaTime = value;
            }
        });
    }
    
    public static MidiFile CreateMidiFile(SongMeta songMeta, List<Note> loadNotesFromMidiFile, byte velocity, int offsetInMillis = 0)
    {
        List<MidiEvent> midiEvents = new();
        uint lastNoteEndInMillis = 0;
        loadNotesFromMidiFile.ForEach(note =>
        {
            uint startInMillis = (uint)(BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.StartBeat) + offsetInMillis);
            if (startInMillis < lastNoteEndInMillis)
            {
                return;
            }
            uint endInMillis = (uint)(BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.EndBeat) + offsetInMillis);
            
            MidiEvent noteOnEvent = new MidiEvent();
            noteOnEvent.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_On;
            noteOnEvent.deltaTime = (uint)startInMillis - lastNoteEndInMillis;
            noteOnEvent.parameter1 = (byte)note.MidiNote;
            noteOnEvent.parameter2 = velocity;
            midiEvents.Add(noteOnEvent);
            
            MidiEvent noteOffEvent = new MidiEvent();
            noteOffEvent.midiChannelEvent = MidiHelper.MidiChannelEvent.Note_Off;
            noteOffEvent.deltaTime = endInMillis - (lastNoteEndInMillis + noteOnEvent.deltaTime);
            noteOffEvent.parameter1 = (byte)note.MidiNote;
            noteOffEvent.parameter2 = velocity;
            midiEvents.Add(noteOffEvent);
            
            lastNoteEndInMillis = endInMillis;
        });

        MidiFile midiFile = MidiFile.CreateEmpty();
        midiFile.MidiHeader.DeltaTiming = 500;
        midiFile.Tracks[0].Programs = new byte[] { 0 };
        midiFile.Tracks[0].DrumPrograms = new byte[] { 0 };
        midiFile.Tracks[0].MidiEvents = midiEvents.ToArray();
        midiFile.Tracks[0].TotalTime = (ulong)midiEvents
            .Select(midiEvent => (double)midiEvent.deltaTime)
            .Sum();
        return midiFile;
    }
}
