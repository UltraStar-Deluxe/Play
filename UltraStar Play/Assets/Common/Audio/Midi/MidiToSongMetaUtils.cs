using System;
using System.Collections.Generic;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
using UnityEngine;

public static class MidiToSongMetaUtils
{
    public static void FillSongMetaWithMidiLyricsAndNotes(SongMeta songMeta)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        MidiFile midiFile = MidiFileUtils.LoadMidiFile(audioUri);
        if (midiFile == null)
        {
            return;
        }
        
        List<TrackAndChannel> tracksAndChannels = MidiFileUtils.GetTracksAndChannels(midiFile);
        if (tracksAndChannels.IsNullOrEmpty())
        {
            return;
        }
        
        TrackAndChannel lyricsTrackAndChannel = FindBestMatchingLyricsTrackAndChannel(midiFile, tracksAndChannels);
        if (lyricsTrackAndChannel == null)
        {
            return;
        }

        MidiTrack midiTrack = midiFile.Tracks[lyricsTrackAndChannel.trackIndex];

        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        List<Note> loadedNotes = LoadNotesFromTrack(
            songMeta,
            midiTrack,
            lyricsTrackAndChannel.channelIndex,
            midiEventToDeltaTimeInMillis,
            midiEventToAbsoluteDeltaTimeInMillis);
        if (loadedNotes.IsNullOrEmpty())
        {
            return;
        }
        
        LoadLyricsFromTrack(
            songMeta,
            midiTrack,
            loadedNotes,
            midiEventToDeltaTimeInMillis,
            midiEventToAbsoluteDeltaTimeInMillis);

        AssignNotesToVoice(
            songMeta,
            loadedNotes,
            Voice.firstVoiceName,
            midiTrack,
            midiEventToDeltaTimeInMillis,
            midiEventToAbsoluteDeltaTimeInMillis);
    }
    
    public static List<Note> LoadNotesFromMidiFile(
        SongMeta songMeta,
        MidiFile midiFile,
        int trackIndex,
        int channelIndex,
        bool importWithLyrics,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        if (trackIndex >= midiFile.Tracks.Length)
        {
            throw new UltraStarPlayException($"No track with index {trackIndex}");
        }

        MidiTrack track = midiFile.Tracks[trackIndex];
        List<Note> loadedNotes = LoadNotesFromTrack(songMeta, track, channelIndex, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        if (loadedNotes.IsNullOrEmpty())
        {
            throw new UltraStarPlayException($"No notes found in channel {channelIndex} of track {trackIndex}");
        }
    
        if (importWithLyrics)
        {
            LoadLyricsFromTrack(songMeta, track, loadedNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        }
        
        Debug.Log("Loaded notes from midi file: " + loadedNotes.Count);
        return loadedNotes;
    }

    private static List<Note> LoadNotesFromTrack(
        SongMeta songMeta,
        MidiTrack track,
        int channelIndex,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        List<Note> loadedNotes = new();

        List<MidiEvent> midiEventsOfChannel = track.MidiEvents
            .Where(midiEvent => midiEvent.Channel == channelIndex)
            .ToList();
        if (midiEventsOfChannel.IsNullOrEmpty())
        {
            throw new UltraStarPlayException($"No midi event in channel {channelIndex}");
        }
        
        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new();
        
        midiEventsOfChannel.ForEach(midiEvent =>
        {
            if (midiEvent.TryGetMidiEventTypeEnum(out MidiEventTypeEnum midiEventTypeEnum) 
                && midiEventTypeEnum == MidiEventTypeEnum.NoteOn)
            {
                HandleStartOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }
    
            if (midiEvent.TryGetMidiEventTypeEnum(out midiEventTypeEnum) 
                && midiEventTypeEnum == MidiEventTypeEnum.NoteOff)
            {
                HandleEndOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction, loadedNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }
        });

        return loadedNotes;
    }
    
    private static void LoadLyricsFromTrack(
        SongMeta songMeta,
        MidiTrack track,
        List<Note> loadedNotes,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        List<Note> notesWithoutText = loadedNotes
            .Where(note => note.Text.IsNullOrEmpty())
            .ToList();
    
        List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(track);
        if (lyricsEvents.IsNullOrEmpty())
        {
            return;
        }
        
        lyricsEvents.ForEach(midiEvent =>
        {
            string midiEventLyrics = MidiFileUtils.GetLyrics(midiEvent);
            if (midiEventLyrics.IsNullOrEmpty())
            {
                return;
            }
    
            midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis);
            int beat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, absoluteDeltaTimeInMillis));
            Note correspondingNote = notesWithoutText.FirstOrDefault(note => SongMetaUtils.IsBeatInNote(note, beat));
            if (correspondingNote != null)
            {
                notesWithoutText.Remove(correspondingNote);
                correspondingNote.SetText(midiEventLyrics);
            }
            else
            {
                // Find best matching note within a tolerance.
                Note bestMatch = notesWithoutText.FindMinElement(note => Math.Abs(note.StartBeat - beat));
                if (bestMatch != null)
                {
                    double distanceInMillis = Math.Abs(bestMatch.StartBeat - beat) * BpmUtils.MillisecondsPerBeat(songMeta);
                    if (distanceInMillis < 1000)
                    {
                        notesWithoutText.Remove(bestMatch);
                        bestMatch.SetText(midiEventLyrics);
                    }
                }
            }
        });
    
        // Normalize text on notes.
        Note lastNote = null;
        foreach (Note note in loadedNotes)
        {
            if (note.Text.Contains("\n"))
            {
                note.SetText(note.Text.Replace("\n", ""));
            }
            
            if (lastNote != null
                && note.Text.StartsWith(" ")
                && !lastNote.Text.EndsWith(" "))
            {
                lastNote.SetText(lastNote.Text + " ");
                note.SetText(note.Text.Substring(1));
            }
            
            lastNote = note;
        }
    }
    
    private static void HandleStartOfNote(
        SongMeta songMeta,
        MidiEvent midiEvent,
        Dictionary<int, Note> midiPitchToNoteUnderConstruction,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        int midiPitch = midiEvent.Data1;
        if (!midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis))
        {
            return;
        }
        
        Note newNote = new();
        int startBeat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, absoluteDeltaTimeInMillis));
        newNote.SetStartAndEndBeat(startBeat, startBeat);
        newNote.SetMidiNote(midiPitch);
        
        if (midiPitchToNoteUnderConstruction.ContainsKey(midiPitch))
        {
            Debug.LogWarning($"A Note with pitch {midiPitch} started but did not end before the next. The note will be ignored.");
        }
        
        midiPitchToNoteUnderConstruction[midiPitch] = newNote;
    }
    
    private static void HandleEndOfNote(
        SongMeta songMeta,
        MidiEvent midiEvent,
        Dictionary<int, Note> midiPitchToNoteUnderConstruction,
        List<Note> loadedNotes,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        int midiPitch = midiEvent.Data1;
        if (!midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis))
        {
            return;
        }
        
        int endBeat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, absoluteDeltaTimeInMillis));
        if (midiPitchToNoteUnderConstruction.TryGetValue(midiPitch, out Note existingNote))
        {
            if (endBeat > existingNote.StartBeat)
            {
                existingNote.SetEndBeat(endBeat);
                loadedNotes.Add(existingNote);
            }
            else
            {
                Debug.LogWarning($"End beat {endBeat} is not after start beat {existingNote.StartBeat}. Skipping this note.");
            }
            midiPitchToNoteUnderConstruction.Remove(midiPitch);
        }
        else
        {
            Debug.LogWarning($"No Note for pitch {MidiUtils.GetAbsoluteName(midiPitch)} is being constructed. Ignoring this Note_Off event at {absoluteDeltaTimeInMillis} ms.");
        }
    }
    
    public static void AssignNotesToVoice(
        SongMeta songMeta,
        List<Note> loadedNotes,
        string voiceName,
        MidiTrack track,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        // Search for line breaks in lyrics of the channel.
        // A line break starts a new sentence.
        List<Note> notesWithoutGroup = loadedNotes.ToList();
        List<List<Note>> noteGroups = new();
        GroupNotesByLineBreak();

        void GroupNotesByLineBreak()
        {
            List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(track);
            if (lyricsEvents.IsNullOrEmpty())
            {
                return;
            }

            lyricsEvents.ForEach(midiEvent =>
            {
                string midiEventLyrics = MidiFileUtils.GetLyrics(midiEvent);
                if (!midiEventLyrics.EndsWith("\n"))
                {
                    // This is not a line break.
                    return;
                }

                midiEventToDeltaTimeInMillis.TryGetValue(midiEvent, out int deltaTimeInMillis);
                int beat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, deltaTimeInMillis));
                List<Note> correspondingNotes = notesWithoutGroup
                    .Where(note => note.StartBeat <= beat)
                    .ToList();
                if (!correspondingNotes.IsNullOrEmpty())
                {
                    noteGroups.Add(correspondingNotes);
                    notesWithoutGroup.RemoveAll(correspondingNotes);
                }
            });
        }

        if (noteGroups.IsNullOrEmpty())
        {
            // No line breaks found in lyrics such that there are no groups.
            // Thus, split into groups here.
            noteGroups = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, loadedNotes);
        }
        
        noteGroups.ForEach(notesGroup =>
        {
            MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, notesGroup, voiceName);
        });
    }
    
    public static TrackAndChannel FindBestMatchingLyricsTrackAndChannel(
        MidiFile midiFile,
        List<TrackAndChannel> trackAndChannels)
    {
        // TODO: Bad performance
        
        if (trackAndChannels.IsNullOrEmpty())
        {
            return null;
        }
        
        using DisposableStopwatch d = new DisposableStopwatch("FindBestMatchingTrackAndChannel took <ms>");
        
        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        int GetAbsoluteDeltaTimeInMillis(MidiEvent midiEvent)
        {
            if (midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis))
            {
                return absoluteDeltaTimeInMillis;
            }

            return 0;
        }
        
        int FindTrackIndexWithLongestLyrics()
        {
            if (trackAndChannels.IsNullOrEmpty())
            {
                return -1;
            }
            if (trackAndChannels.Count == 1)
            {
                return 0;
            }
            
            int trackIndexWithLongestLyrics = trackAndChannels.FirstOrDefault().trackIndex;
            int longestLyricsLength = 0;
            foreach (TrackAndChannel trackAndChannel in trackAndChannels)
            {
                MidiTrack midiTrack = midiFile.Tracks[trackAndChannel.trackIndex];
                string lyrics = MidiFileUtils.GetLyrics(midiTrack);
                if (!lyrics.IsNullOrEmpty()
                    && lyrics.Length > longestLyricsLength)
                {
                    trackIndexWithLongestLyrics = trackAndChannel.trackIndex;
                    longestLyricsLength = lyrics.Length;
                }
            }

            return trackIndexWithLongestLyrics;
        }

        int bestTrackIndex = FindTrackIndexWithLongestLyrics();
        if (bestTrackIndex < 0)
        {
            return trackAndChannels.FirstOrDefault();
        }

        double GetMidiEventAbsoluteTimeDistance(MidiEvent a, MidiEvent b)
        {
            if (a == null
                && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return GetAbsoluteDeltaTimeInMillis(b);
            }

            if (b == null)
            {
                return GetAbsoluteDeltaTimeInMillis(a);
            }
            
            return Mathf.Abs(GetAbsoluteDeltaTimeInMillis(a) - GetAbsoluteDeltaTimeInMillis(b));
        }
        
        int FindChannelIndexWithBestMatchingNotes()
        {
            List<int> channelIndexes = trackAndChannels
                .Where(it => it.trackIndex == bestTrackIndex)
                .Select(it => it.channelIndex)
                .Distinct()
                .ToList();
            if (channelIndexes.IsNullOrEmpty())
            {
                return -1;
            }
            if (channelIndexes.Count == 1)
            {
                return channelIndexes[0];
            }
            
            // For each channel, calculate difference to lyrics events. Return the channel with smallest difference.
            MidiTrack bestTrack = midiFile.Tracks[bestTrackIndex];
            List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(bestTrack);
            
            Dictionary<int, double> channelIndexToDistance = new();
            foreach (int channelIndex in channelIndexes)
            {
                List<MidiEvent> noteEventsOfChannel = bestTrack.MidiEvents
                    .Where(midiEvent => midiEvent.Channel == (byte)channelIndex
                                        && midiEvent.TryGetMidiEventTypeEnum(out MidiEventTypeEnum midiEventTypeEnum)
                                            && midiEventTypeEnum == MidiEventTypeEnum.NoteOn)
                    .ToList();
            
                double distanceOfChannel = 0;
                foreach (MidiEvent lyricsEvent in lyricsEvents)
                {
                    MidiEvent closestNoteOfLyricsEvent = noteEventsOfChannel.FindMinElement(noteEvent =>
                        GetMidiEventAbsoluteTimeDistance(lyricsEvent, noteEvent));
                    if (closestNoteOfLyricsEvent == null)
                    {
                        // Add unmatched lyrics event to distance.
                        distanceOfChannel += GetAbsoluteDeltaTimeInMillis(lyricsEvent);
                    }
            
                    noteEventsOfChannel.Remove(closestNoteOfLyricsEvent);
                    double distanceOfNote = GetMidiEventAbsoluteTimeDistance(lyricsEvent, closestNoteOfLyricsEvent);
                    distanceOfChannel += distanceOfNote;
                }
            
                // Add unmatched notes to distance
                distanceOfChannel += noteEventsOfChannel.Sum(noteEvent => GetAbsoluteDeltaTimeInMillis(noteEvent));
                
                channelIndexToDistance[channelIndex] = distanceOfChannel;
            }
            
            int channelIndexWithSmallestDistance = channelIndexToDistance.FindMinElement(entry => entry.Value).Key;
            return channelIndexWithSmallestDistance;
        }

        int bestChannelIndex = FindChannelIndexWithBestMatchingNotes();
        if (bestChannelIndex < 0)
        {
            return null;
        }
        return new TrackAndChannel(bestTrackIndex, bestChannelIndex);
    }
}
