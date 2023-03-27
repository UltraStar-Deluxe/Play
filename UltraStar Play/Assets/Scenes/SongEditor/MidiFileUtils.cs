using System;
using System.Collections.Generic;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
using UnityEngine;

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
                .Where(midiEvent =>
                {
                    if (midiEvent.TryGetMidiEventTypeEnum(out MidiEventTypeEnum midiEventTypeEnum))
                    {
                        return midiEventTypeEnum == MidiEventTypeEnum.NoteOn;
                    }

                    return false;
                })
                .ToList();
        }
        return midiEvents.Select(midiEvent => midiEvent.Channel)
            .Distinct()
            .OrderBy(channelIndex => channelIndex)
            .ToList();
    }

    public static List<MidiEvent> GetLyricsEvents(MidiTrack track)
    {
        List<MidiEvent> lyricsEvents = track.MidiEvents
            .Where(e => e.TryGetMetaEventTypeEnum(out MetaEventTypeEnum metaEventTypeEnum)
                        && metaEventTypeEnum == MetaEventTypeEnum.LyricText)
            .ToList();
        List<MidiEvent> textEvent = track.MidiEvents
            .Where(e => e.TryGetMetaEventTypeEnum(out MetaEventTypeEnum metaEventTypeEnum)
                        && metaEventTypeEnum == MetaEventTypeEnum.TextEvent)
            .ToList();
        List<MidiEvent> markerTextEvents = track.MidiEvents
            .Where(e => e.TryGetMetaEventTypeEnum(out MetaEventTypeEnum metaEventTypeEnum)
                        && metaEventTypeEnum == MetaEventTypeEnum.MarkerText)
            .ToList();
        List<MidiEvent> actualLyricsEvents = new List<List<MidiEvent>> { lyricsEvents, textEvent, markerTextEvents }
            .FindMaxElement(events => events.Count);
        
        return actualLyricsEvents;
    }

    public static string GetLyrics(MidiEvent midiEvent)
    {
        MetaTextEvent metaTextEvent = midiEvent as MetaTextEvent;
        if (metaTextEvent == null)
        {
            return null;
        }
        
        string rawLyrics = metaTextEvent.Text as string;
        return rawLyrics
            .Replace("\r", "\n")   
            .Replace("/", "\n")
            .Replace("\\", "\n");
    }
    
    public static string GetLyrics(MidiTrack track)
    {
        List<MidiEvent> lyricsEvents = GetLyricsEvents(track);
        return lyricsEvents
            .Select(midiEvent => GetLyrics(midiEvent))
            .JoinWith("");
    }
    
    public static Dictionary<MidiEvent, int> GetMidiEventToAbsoluteTime(MidiTrack track)
    {
        Dictionary<int, int> channelIndexToTime = new();
        Dictionary<MidiEvent, int> midiEventToTime = new();
        foreach (MidiEvent midiEvent in track.MidiEvents)
        {
            if (!channelIndexToTime.ContainsKey(midiEvent.Channel))
            {
                channelIndexToTime[midiEvent.Channel] = 0;
            }
        
            channelIndexToTime[midiEvent.Channel] += midiEvent.DeltaTime;
            
            midiEventToTime[midiEvent] = channelIndexToTime[midiEvent.Channel];
        }
        
        return midiEventToTime;
    }
    
    public static void SetFirstDeltaTimeTo(MidiFile midiFile, int trackIndex, int newDeltaTime)
    {
        // Set delta time of fist note 0 to to start immediately.
        MidiTrack midiTrack = midiFile.Tracks[trackIndex];
        MidiEvent firstNoteOnEvent = midiTrack.MidiEvents.FirstOrDefault(midiEvent =>
            midiEvent.TryGetMidiEventTypeEnum(out MidiEventTypeEnum midiEventTypeEnum)
            && midiEventTypeEnum is MidiEventTypeEnum.NoteOn);
        if (firstNoteOnEvent != null)
        {
            firstNoteOnEvent.DeltaTime = newDeltaTime;
        }
    }
    
    public static int GetMidiFileLengthInMillis(MidiFile midiFile)
    {
        int lengthInMillis = midiFile.Tracks
            .Select(track => track.EndTime)
            .Max();
        return lengthInMillis;
    }
    
    public static MidiFile CreateMidiFile(SongMeta songMeta, List<Note> loadNotesFromMidiFile, byte velocity, int offsetInMillis = 0)
    {
        List<MidiEvent> midiEvents = new();
        int lastNoteEndInMillis = 0;
        loadNotesFromMidiFile.ForEach(note =>
        {
            int startInMillis = (int)(BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.StartBeat) + offsetInMillis);
            if (startInMillis < lastNoteEndInMillis)
            {
                return;
            }
            int endInMillis = (int)(BpmUtils.BeatToMillisecondsInSongWithoutGap(songMeta, note.EndBeat) + offsetInMillis);

            int deltaInMillis = startInMillis - lastNoteEndInMillis;
            
            int noteLengthInMillis = endInMillis - startInMillis;
            AddNoteOnOffEvents(midiEvents, deltaInMillis, noteLengthInMillis, (byte)note.MidiNote, velocity);

            lastNoteEndInMillis = endInMillis;
        });
        
        MidiFile midiFile = new();
        midiFile.Tracks[0].MidiEvents = midiEvents.ToArray();
        return midiFile;
    }
    
    private static void AddNoteOnOffEvents(List<MidiEvent> midiEvents, int noteOnDeltaTimeInMillis, int noteLengthInMillis, byte pitch, byte velocity)
    {
        MidiEvent noteOnEvent = CreateNoteOnEvent(noteOnDeltaTimeInMillis, 0, pitch, velocity);
        midiEvents.Add(noteOnEvent);
            
        MidiEvent noteOffEvent = CreateNoteOffEvent(noteLengthInMillis, 0, pitch, velocity);
        midiEvents.Add(noteOffEvent);
    }

    public static MidiEvent CreateNoteOnEvent(int deltaTime, byte channel, byte pitch, byte velocity)
    {
        int status = 0x90 | channel;
        return new MidiEvent(deltaTime, (byte)status, pitch, velocity);
    }

    public static MidiEvent CreateNoteOffEvent(int deltaTime, byte channel, byte pitch, byte velocity)
    {
        int status = 0x80 | channel;
        return new MidiEvent(deltaTime, (byte)status, pitch, velocity);
    }

    public static void CalculateMidiEventTimesInMillis(
        MidiFile midiFile,
        out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        midiEventToDeltaTimeInMillis = new();
        midiEventToAbsoluteDeltaTimeInMillis = new();
        
        // Create combined MidiTrack if needed (some events of other channels may be important, e.g. tempo change)
        MidiTrack midiTrack;
        Dictionary<MidiEvent, MidiEvent> combinedToOriginalMidiEvent = null;
        if (midiFile.Tracks.Length > 1 || midiFile.Tracks[0].EndTime == 0)
        {
            midiTrack = CombineTracks(midiFile, out combinedToOriginalMidiEvent);
        }
        else
        {
            midiTrack = midiFile.Tracks[0];
        }
        MidiEvent[] midiEvents = midiTrack.MidiEvents;
        
        double BPM = 120.0;
        double absoluteDeltaInSeconds = 0.0;
        foreach (MidiEvent midiEvent in midiEvents)
        {
            MidiEvent originalMidiEvent;
            if (combinedToOriginalMidiEvent != null)
            {
                combinedToOriginalMidiEvent.TryGetValue(midiEvent, out originalMidiEvent);
            }
            else
            {
                originalMidiEvent = midiEvent;
            }

            if (originalMidiEvent == null)
            {
                Debug.LogWarning($"Missing original MidiEvent for {midiEvent}");
                continue;
            }
            
            double deltaInSeconds = midiEvent.DeltaTime * (60.0 / (BPM * midiFile.Division));
            absoluteDeltaInSeconds += deltaInSeconds;
            midiEventToDeltaTimeInMillis[originalMidiEvent] = (int)(deltaInSeconds * 1000);
            midiEventToAbsoluteDeltaTimeInMillis[originalMidiEvent] = (int)(absoluteDeltaInSeconds * 1000);
            
            //Update tempo
            if (midiEvent.Command == 0xFF && midiEvent.Data1 == 0x51)
            {
                BPM = Math.Round(MidiHelper.MicroSecondsPerMinute / (double)((MetaNumberEvent)midiEvent).Value, 2);
            }
        }
    }
    
    private static MidiTrack CombineTracks(
        MidiFile midiFile,
        out Dictionary<MidiEvent, MidiEvent> combinedToOriginalMidiEvent)
    {
        combinedToOriginalMidiEvent = new();
        
        //create a new track of the appropriate size
        MidiTrack finalTrack = MergeTracks(midiFile);
        MidiEvent[][] absevents = new MidiEvent[midiFile.Tracks.Length][];
        //we have to convert delta times to absolute delta times
        for (int x = 0; x < absevents.Length; x++)
        {
            absevents[x] = new MidiEvent[midiFile.Tracks[x].MidiEvents.Length];
            for (int x2 = 0, totalDeltaTime = 0; x2 < absevents[x].Length; x2++)
            {//create copies
                absevents[x][x2] = midiFile.Tracks[x].MidiEvents[x2];
                totalDeltaTime += absevents[x][x2].DeltaTime;
                absevents[x][x2].DeltaTime = totalDeltaTime;
            }
        }
        //sort by absolute delta time also makes sure events occur in order of track and when they are recieved. 
        int eventcount = 0;
        int delta = 0;
        int nextdelta = int.MaxValue;
        int[] counters = new int[absevents.Length];
        while (eventcount < finalTrack.MidiEvents.Length)
        {
            for (int x = 0; x < absevents.Length; x++)
            {
                while (counters[x] < absevents[x].Length && absevents[x][counters[x]].DeltaTime == delta)
                {
                    finalTrack.MidiEvents[eventcount] = absevents[x][counters[x]];
                    
                    combinedToOriginalMidiEvent[finalTrack.MidiEvents[eventcount]] = absevents[x][counters[x]];

                    eventcount++;
                    counters[x]++;
                }
                if (counters[x] < absevents[x].Length && absevents[x][counters[x]].DeltaTime < nextdelta)
                    nextdelta = absevents[x][counters[x]].DeltaTime;
            }
            delta = nextdelta;
            nextdelta = int.MaxValue;
        }
        //set total time
        finalTrack.EndTime = finalTrack.MidiEvents[finalTrack.MidiEvents.Length - 1].DeltaTime;
        //put back into regular delta time
        for (int x = 0, deltadiff = 0; x < finalTrack.MidiEvents.Length; x++)
        {
            int oldtime = finalTrack.MidiEvents[x].DeltaTime;
            finalTrack.MidiEvents[x].DeltaTime -= deltadiff;
            deltadiff = oldtime;
        }

        return finalTrack;
    }

    private static MidiTrack MergeTracks(MidiFile midiFile)
    {
        int eventCount = 0;
        int notesPlayed = 0;
        int activeChannels = 0;
        List<byte> programsUsed = new List<byte>();
        List<byte> drumprogramsUsed = new List<byte>();
        //Loop to get track info
        for (int x = 0; x < midiFile.Tracks.Length; x++)
        {
            eventCount += midiFile.Tracks[x].MidiEvents.Length;
            notesPlayed += midiFile.Tracks[x].NoteOnCount;

            foreach (byte p in midiFile.Tracks[x].Instruments)
            {
                if (!programsUsed.Contains(p))
                    programsUsed.Add(p);
            }
            foreach (byte p in midiFile.Tracks[x].DrumInstruments)
            {
                if (!drumprogramsUsed.Contains(p))
                    drumprogramsUsed.Add(p);
            }
            activeChannels |= midiFile.Tracks[x].ActiveChannels;
        }
        MidiTrack track = new MidiTrack(programsUsed.ToArray(), drumprogramsUsed.ToArray(), new MidiEvent[eventCount]);
        track.NoteOnCount = notesPlayed;
        track.ActiveChannels = activeChannels;
        return track;
    }
}
