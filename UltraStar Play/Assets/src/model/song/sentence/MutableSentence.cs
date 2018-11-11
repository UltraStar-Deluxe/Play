using System;
using System.Collections.Generic;

// this should be internal but tests become impossible
public class MutableSentence
{
    private readonly List<Note> m_notes = new List<Note>();
    private uint m_linebreakBeat = 0;

    public void AddNote(Note note)
    {
        if (note == null)
        {
            throw new ArgumentNullException("note");
        }
        m_notes.Add(note);
    }

    public bool IsEmpty()
    {
        return m_notes.Count == 0;
    }

    public uint GetStartBeat()
    {
        return m_notes[0].GetStartBeat();
    }

    public uint GetEndBeat()
    {
        Note lastNote = m_notes[m_notes.Count-1];
        return lastNote.GetStartBeat() + lastNote.GetLength() - 1;
    }

    public List<Note> GetNotes()
    {
        return m_notes;
    }

    public void SetLinebreakBeat(uint beat)
    {
        m_linebreakBeat = beat;
    }

    public uint GetLinebreakBeat()
    {
        return m_linebreakBeat;
    }

    // this (deliberately) does not do anything with the linebreak
    public void AssertValidNotes()
    {
        // assert sorted notes
        m_notes.Sort((a, b) => a.GetStartBeat().CompareTo(b.GetStartBeat()));
        // assert non-overlapping notes
        for (int i = 0; i < m_notes.Count - 1; i++)
        {
            if (m_notes[i].GetStartBeat() + m_notes[i].GetLength() > m_notes[i+1].GetStartBeat())
            {
                throw new SongBuilderException("The notes starting at beats " + m_notes[i].GetStartBeat() + " and " + m_notes[i+1].GetStartBeat() + " are overlapping");
            }
        }
    }
}
