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
    
    public static List<int> GetChannelIndexes(MidiTrack track)
    {
        if (track == null)
        {
            return new();
        }
        return track.MidiEvents
            .Select(midiEvent => (int)midiEvent.channel)
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
}
