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

        List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(midiFile);
        if (lyricsEvents.IsNullOrEmpty())
        {
            return;
        }

        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        TrackAndChannel lyricsTrackAndChannel = FindTrackAndChannelWithBestMatchingNotesForLyricsEvents(midiFile, lyricsEvents, tracksAndChannels, midiEventToAbsoluteDeltaTimeInMillis);
        if (lyricsTrackAndChannel == null)
        {
            return;
        }
        MidiTrack track = midiFile.Tracks[lyricsTrackAndChannel.trackIndex];

        List<Note> loadedNotes = LoadLyricsFromMidiFile(songMeta, midiFile, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        MoveNotesToPitchAndPositionOfTrack(songMeta, track, lyricsTrackAndChannel.channelIndex, loadedNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);

        AssignNotesToVoice(
            songMeta,
            loadedNotes,
            EVoiceId.P1,
            track,
            midiEventToDeltaTimeInMillis,
            midiEventToAbsoluteDeltaTimeInMillis);
    }

    public static List<Note> LoadNotesFromMidiFile(
        SongMeta songMeta,
        MidiFile midiFile,
        int trackIndex,
        int channelIndex,
        bool importLyrics,
        bool importNotes,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        if (trackIndex >= midiFile.Tracks.Length)
        {
            throw new MidiToSongMetaException($"No track with index {trackIndex}");
        }

        using DisposableStopwatch d = new("LoadNotesFromMidiFile took <ms>");

        MidiTrack track = midiFile.Tracks[trackIndex];

        List<Note> loadedNotes;
        if (importLyrics)
        {
            // Import all lyrics as notes, then try to find matching note in track to set pitch and position.
            loadedNotes = LoadLyricsFromMidiFile(songMeta, midiFile, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);

            if (importNotes)
            {
                MoveNotesToPitchAndPositionOfTrack(songMeta, track, channelIndex, loadedNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }
        }
        else if (importNotes)
        {
            // Import notes from track
            loadedNotes = LoadNotesFromTrack(songMeta, track, channelIndex, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        }
        else
        {
            return new List<Note>();
        }

        if (loadedNotes.IsNullOrEmpty())
        {
            throw new MidiToSongMetaException($"No notes found in channel {channelIndex} of track {trackIndex}");
        }

        Debug.Log($"Loaded {loadedNotes.Count} notes from midi file");
        return loadedNotes;
    }

    private static void MoveNotesToPitchAndPositionOfTrack(
        SongMeta songMeta,
        MidiTrack track,
        int channelIndex,
        List<Note> loadedNotes,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        List<Note> notesOfTrack = LoadNotesFromTrack(songMeta, track, channelIndex, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        if (notesOfTrack.IsNullOrEmpty())
        {
            return;
        }

        List<Note> unusedNotesOfTrack = new List<Note>(notesOfTrack);
        List<Note> unusedLoadedNotes = new List<Note>(loadedNotes);

        Note lastMatchingNoteOfTrack = null;
        foreach (Note note in loadedNotes)
        {
            Note matchingNoteOfTrack = FindMatchingNoteOfTrack(note, unusedNotesOfTrack);
            if (matchingNoteOfTrack == null)
            {
                if (lastMatchingNoteOfTrack != null)
                {
                    // Only set the pitch, not the position
                    note.SetMidiNote(lastMatchingNoteOfTrack.MidiNote);
                }
                continue;
            }

            // Remove all previous notes of track because these can never qualify for the remaining notes.
            unusedNotesOfTrack.RemoveAll(n => n.EndBeat < matchingNoteOfTrack.EndBeat);
            unusedLoadedNotes.Remove(note);

            note.SetMidiNote(matchingNoteOfTrack.MidiNote);
            note.SetStartAndEndBeat(matchingNoteOfTrack.StartBeat, matchingNoteOfTrack.EndBeat);

            lastMatchingNoteOfTrack = matchingNoteOfTrack;
        }
    }

    private static Note FindMatchingNoteOfTrack(Note note, List<Note> unusedNotesOfTrack)
    {
        Note matchingNote = unusedNotesOfTrack.FirstOrDefault(noteOfTrack =>
            SongMetaUtils.IsBeatInNote(noteOfTrack, note.StartBeat));
        return matchingNote;
    }

    private static List<Note> LoadLyricsFromMidiFile(SongMeta songMeta, MidiFile midiFile, Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis, Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        using DisposableStopwatch d = new("LoadLyricsFromMidiFile took <ms>");

        int defaultMidiNote = SettingsManager.Instance.Settings.SongEditorSettings.DefaultPitchForCreatedNotes;
        int defaultUltraStarTxtPitch = MidiUtils.GetUltraStarTxtPitch(defaultMidiNote);

        List<MidiEvent> lyricsEvents = MidiFileUtils.GetLyricsEvents(midiFile);
        if (lyricsEvents.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        // Create notes for lyrics, make them as wide as possible, and normalize their text.
        List<Note> loadedNotes = LoadNotesFromLyricsEvents(songMeta, lyricsEvents, defaultUltraStarTxtPitch, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
        ExpandNotesToUseAvailableSpace(songMeta, loadedNotes);
        NormalizeTextOnNotes(loadedNotes);

        return loadedNotes;
    }

    private static void NormalizeTextOnNotes(List<Note> loadedNotes)
    {
        Note lastNote = null;
        foreach (Note note in loadedNotes)
        {
            // Remove line break
            if (note.Text.Contains("\n"))
            {
                note.SetText(note.Text.Replace("\n", ""));
            }

            // Move space to end of word
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

    private static void ExpandNotesToUseAvailableSpace(SongMeta songMeta, List<Note> notes)
    {
        for (int i = 0; i < notes.Count; i++)
        {
            int nextNoteIndex = i + 1;
            if (nextNoteIndex >= notes.Count)
            {
                continue;
            }

            Note note = notes[i];
            Note nextNote = notes[nextNoteIndex];
            int availableSpaceInBeats = nextNote.StartBeat - note.EndBeat - 1;
            if (availableSpaceInBeats < 1)
            {
                continue;
            }

            int targetLengthInMillis = 100;
            int targetLengthInBeats = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, targetLengthInMillis);
            int newLengthInBeats = Math.Min(targetLengthInBeats, availableSpaceInBeats);
            if (newLengthInBeats > 0)
            {
                note.SetLength(newLengthInBeats);
            }
        }
    }

    private static List<Note> LoadNotesFromLyricsEvents(
        SongMeta songMeta,
        List<MidiEvent> lyricsEvents,
        int ultraStarTxtPitch,
        Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        List<Note> loadedNotes = new();
        foreach(MidiEvent midiEvent in lyricsEvents)
        {
            string midiEventLyrics = MidiFileUtils.GetLyrics(midiEvent);
            if (midiEventLyrics.IsNullOrEmpty()
                || midiEventLyrics == " "
                || midiEventLyrics == "\t"
                || midiEventLyrics == "\n")
            {
                continue;
            }

            midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis);
            int beat = (int)Math.Round(SongMetaBpmUtils.MillisToBeats(songMeta, absoluteDeltaTimeInMillis));
            Note note = new Note(ENoteType.Normal, beat, 1, ultraStarTxtPitch, midiEventLyrics);
            loadedNotes.Add(note);
        }

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
            throw new MidiToSongMetaException($"No midi event in channel {channelIndex}");
        }

        Dictionary<int, Note> midiPitchToNoteUnderConstruction = new();

        midiEventsOfChannel.ForEach(midiEvent =>
        {
            if (midiEvent.TryGetMidiEventTypeEnumFast(out MidiEventTypeEnum midiEventTypeEnum)
                && midiEventTypeEnum == MidiEventTypeEnum.NoteOn)
            {
                HandleStartOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }

            if (midiEvent.TryGetMidiEventTypeEnumFast(out midiEventTypeEnum)
                && midiEventTypeEnum == MidiEventTypeEnum.NoteOff)
            {
                HandleEndOfNote(songMeta, midiEvent, midiPitchToNoteUnderConstruction, loadedNotes, midiEventToDeltaTimeInMillis, midiEventToAbsoluteDeltaTimeInMillis);
            }
        });

        return loadedNotes;
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
        int startBeat = (int)Math.Round(SongMetaBpmUtils.MillisToBeats(songMeta, absoluteDeltaTimeInMillis));
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

        int endBeat = (int)Math.Round(SongMetaBpmUtils.MillisToBeats(songMeta, absoluteDeltaTimeInMillis));
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
        EVoiceId voiceId,
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
                int beat = (int)Math.Round(SongMetaBpmUtils.MillisToBeats(songMeta, deltaTimeInMillis));
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
            MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, notesGroup, voiceId);
        });
    }

    public static TrackAndChannel FindTrackAndChannelWithBestMatchingNotesForLyricsEvents(
        MidiFile midiFile,
        List<MidiEvent> lyricsEvents,
        List<TrackAndChannel> trackAndChannels,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        if (trackAndChannels.IsNullOrEmpty())
        {
            return null;
        }

        using DisposableStopwatch d = new("FindBestMatchingTrackAndChannel took <ms>");

        MidiTrack bestTrack = MidiFileUtils.FindTrackWithLongestLyrics(midiFile);
        if (bestTrack == null)
        {
            return trackAndChannels.FirstOrDefault();
        }
        int bestTrackIndex = midiFile.Tracks.IndexOf(bestTrack);

        int bestChannelIndex = FindChannelIndexWithBestMatchingNotesForLyricsEvents(bestTrack, lyricsEvents, midiEventToAbsoluteDeltaTimeInMillis);
        if (bestChannelIndex < 0)
        {
            return null;
        }
        return new TrackAndChannel(bestTrackIndex, bestChannelIndex);
    }

    private static int FindChannelIndexWithBestMatchingNotesForLyricsEvents(
        MidiTrack track,
        List<MidiEvent> lyricsEvents,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        List<int> channelIndexes = MidiFileUtils.GetChannelIndexes(track, true);
        if (channelIndexes.IsNullOrEmpty())
        {
            return -1;
        }
        if (channelIndexes.Count == 1)
        {
            return channelIndexes[0];
        }

        // For each channel, calculate difference to lyrics events.
        // Then return the channel with smallest difference.
        Dictionary<int, double> channelIndexToDistance = new();
        foreach (int channelIndex in channelIndexes)
        {
            List<MidiEvent> noteEventsOfChannel = track.MidiEvents
                .Where(midiEvent => midiEvent.Channel == (byte)channelIndex
                                    && midiEvent.TryGetMidiEventTypeEnumFast(out MidiEventTypeEnum midiEventTypeEnum)
                                        && midiEventTypeEnum == MidiEventTypeEnum.NoteOn)
                .ToList();

            Dictionary<MidiEvent, MidiEvent> lyricsEventToNoteEvent = new();

            double distanceOfChannel = 0;
            foreach (MidiEvent lyricsEvent in lyricsEvents)
            {
                if (noteEventsOfChannel.IsNullOrEmpty())
                {
                    break;
                }

                MidiEvent closestNoteOfLyricsEvent = noteEventsOfChannel.FindMinElement(noteEvent =>
                    GetMidiEventAbsoluteTimeDistance(lyricsEvent, noteEvent, midiEventToAbsoluteDeltaTimeInMillis));
                if (closestNoteOfLyricsEvent == null)
                {
                    // Add unmatched lyrics event to distance.
                    distanceOfChannel += GetAbsoluteDeltaTimeInMillis(lyricsEvent, midiEventToAbsoluteDeltaTimeInMillis);
                }

                // Following matches must be behind this. Thus, remove all notes up to the current match.
                while (!noteEventsOfChannel.IsNullOrEmpty()
                       && noteEventsOfChannel[0] != closestNoteOfLyricsEvent)
                {
                    noteEventsOfChannel.RemoveAt(0);
                }

                double distanceOfNote = GetMidiEventAbsoluteTimeDistance(lyricsEvent, closestNoteOfLyricsEvent, midiEventToAbsoluteDeltaTimeInMillis);
                distanceOfChannel += distanceOfNote;
            }

            // Add unmatched notes to distance
            distanceOfChannel += noteEventsOfChannel.Sum(noteEvent => GetAbsoluteDeltaTimeInMillis(noteEvent, midiEventToAbsoluteDeltaTimeInMillis));

            channelIndexToDistance[channelIndex] = distanceOfChannel;
        }

        int channelIndexWithSmallestDistance = channelIndexToDistance.FindMinElement(entry => entry.Value).Key;
        return channelIndexWithSmallestDistance;
    }

    private static double GetMidiEventAbsoluteTimeDistance(
        MidiEvent a,
        MidiEvent b,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        if (a == null
            && b == null)
        {
            return 0;
        }

        if (a == null)
        {
            return GetAbsoluteDeltaTimeInMillis(b, midiEventToAbsoluteDeltaTimeInMillis);
        }

        if (b == null)
        {
            return GetAbsoluteDeltaTimeInMillis(a, midiEventToAbsoluteDeltaTimeInMillis);
        }

        return Mathf.Abs(GetAbsoluteDeltaTimeInMillis(a, midiEventToAbsoluteDeltaTimeInMillis) - GetAbsoluteDeltaTimeInMillis(b, midiEventToAbsoluteDeltaTimeInMillis));
    }

    private static int GetAbsoluteDeltaTimeInMillis(
        MidiEvent midiEvent,
        Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis)
    {
        if (midiEventToAbsoluteDeltaTimeInMillis.TryGetValue(midiEvent, out int absoluteDeltaTimeInMillis))
        {
            return absoluteDeltaTimeInMillis;
        }

        return 0;
    }
}
