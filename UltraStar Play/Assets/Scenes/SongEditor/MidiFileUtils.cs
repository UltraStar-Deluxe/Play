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
        return new List<int>();
        // if (track == null)
        // {
        //     return new();
        // }
        //
        // List<MidiEvent> midiEvents = track.MidiEvents.ToList();
        // if (onlyWithNotes)
        // {
        //     midiEvents = midiEvents
        //         .Where(midiEvent => midiEvent.midiChannelEvent is MidiHelper.MidiChannelEvent.Note_On)
        //         .ToList();
        // }
        // return midiEvents.Select(midiEvent => (int)midiEvent.channel)
        //     .Distinct()
        //     .OrderBy(channelIndex => channelIndex)
        //     .ToList();
    }

    public static List<MidiEvent> GetLyricsEvents(MidiTrack track)
    {
        return new List<MidiEvent>();
        // List<MidiEvent> lyricsEvents = track.MidiEvents
        //     .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Lyric_Text)
        //     .ToList();
        // List<MidiEvent> textEvent = track.MidiEvents
        //     .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Text_Event)
        //     .ToList();
        // List<MidiEvent> markerTextEvents = track.MidiEvents
        //     .Where(e => e.isMetaEvent() && e.midiMetaEvent == MidiHelper.MidiMetaEvent.Marker_Text)
        //     .ToList();
        // List<MidiEvent> actualLyricsEvents = new List<List<MidiEvent>> { lyricsEvents, textEvent, markerTextEvents }
        //     .FindMaxElement(events => events.Count);
        //
        // return actualLyricsEvents;
    }

    public static string GetLyrics(MidiEvent midiEvent)
    {
        // if (midiEvent.Parameters.IsNullOrEmpty())
        // {
        //     return null;
        // }
        //
        // foreach (object parameter in midiEvent.Parameters)
        // {
        //     if (parameter is string)
        //     {
        //         string rawLyrics = parameter as string;
        //         return rawLyrics
        //             .Replace("\r", "\n")   
        //             .Replace("/", "\n")
        //             .Replace("\\", "\n");
        //     }
        // }

        return null;
    }
    
    public static string GetLyrics(MidiTrack track)
    {
        List<MidiEvent> lyricsEvents = GetLyricsEvents(track);
        return lyricsEvents
            .Select(midiEvent => GetLyrics(midiEvent))
            .JoinWith("");
    }
    
    public static Dictionary<MidiEvent, uint> GetMidiEventToAbsoluteTime(MidiTrack track)
    {
        return new Dictionary<MidiEvent, uint>();
        // Dictionary<byte, uint> channelIndexToTime = new();
        // Dictionary<MidiEvent, uint> midiEventToTime = new();
        // foreach (MidiEvent midiEvent in track.MidiEvents)
        // {
        //     if (!channelIndexToTime.ContainsKey(midiEvent.channel))
        //     {
        //         channelIndexToTime[midiEvent.channel] = 0;
        //     }
        //
        //     channelIndexToTime[midiEvent.channel] += midiEvent.deltaTime;
        //     
        //     midiEventToTime[midiEvent] = channelIndexToTime[midiEvent.channel];
        // }
        //
        // return midiEventToTime;
    }
    
    public static int GetDeltaTimeInMillis(MidiEvent midiEvent)
    {
        return 0;
        // // TODO: This is just wrong. MidiFile has a deltaTiming property.
        // uint deltaTimeInSamples = midiEvent.deltaTime;
        // int deltaTimeInMillis = (int)Math.Round(deltaTimeInSamples / (MidiManager.midiStreamSampleRateHz / 1000.0));
        // return deltaTimeInMillis;
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
        MidiEvent noteOnEvent = MidiFileUtils.CreateNoteOnEvent(noteOnDeltaTimeInMillis, 0, pitch, velocity);
        midiEvents.Add(noteOnEvent);
            
        MidiEvent noteOffEvent = MidiFileUtils.CreateNoteOffEvent(noteLengthInMillis, 0, pitch, velocity);
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
}
