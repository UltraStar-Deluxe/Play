using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SpaceBetweenNotesUtils
{
    public const int DefaultSpaceBetweenNotesInMillis = 150;

    public static void AddSpaceInMillisBetweenNotes(IReadOnlyCollection<Note> notes, int millis, SongMeta songMeta)
    {
        double beats = SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, millis);
        if (beats < 1)
        {
            return;
        }

        AddSpaceInBeatsBetweenNotes(notes, (int)beats);
    }

    public static void AddSpaceInBeatsBetweenNotes(IReadOnlyCollection<Note> notes, int spaceInBeats)
    {
        if (spaceInBeats <= 0)
        {
            return;
        }

        Debug.Log("AddSpaceInBeatsBetweenNotes - spaceInBeats: " + spaceInBeats);

        // Sort notes
        List<Note> sortedNotes = notes.OrderBy(note => note.StartBeat).ToList();
        // Check if distance to following note satisfies the desired space. If not, then shorten note.
        for (int i = 0; i < sortedNotes.Count - 1; i++)
        {
            Note note = sortedNotes[i];
            Note followingNote = sortedNotes[i + 1];
            int distance = followingNote.StartBeat - note.EndBeat;
            if (distance < spaceInBeats)
            {
                int newEndBeat = followingNote.StartBeat - spaceInBeats;
                if (newEndBeat > note.StartBeat)
                {
                    note.SetEndBeat(newEndBeat);
                }
                else
                {
                    // Shorten as much as possible without removing the note
                    note.SetLength(1);
                }
            }
        }
    }

    public static void ShortenNotesByMillis(IReadOnlyCollection<Note> notes, int millis, SongMeta songMeta)
    {
        if (notes.IsNullOrEmpty())
        {
            return;
        }

        double lengthInBeats = SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, millis);
        if (lengthInBeats < 1)
        {
            return;
        }

        ShortenNotesByBeats(notes, (int)lengthInBeats);
    }

    public static void ShortenNotesByBeats(IReadOnlyCollection<Note> notes, int lengthInBeats)
    {
        // Remove half from start and end of note
        int halfLengthInBeats = lengthInBeats / 2;
        notes.ForEach(currentNote =>
            {
                if (currentNote.Length > lengthInBeats + 1)
                {
                    currentNote.SetStartAndEndBeat(currentNote.StartBeat + halfLengthInBeats, currentNote.EndBeat - halfLengthInBeats);
                }
            });
    }
}
