using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentence
{
    private readonly List<Note> m_notes;
    private readonly uint m_linebreakBeat;

    public Sentence(List<Note> notes, uint linebreakBeat)
    {
        if (notes == null || notes.Count < 1)
        {
            throw new UnityException("notes is null or empty!");
        }
        m_notes = notes;
        m_linebreakBeat = linebreakBeat;
    }

    public List<Note> GetNotes()
    {
        return m_notes;
    }

    public uint GetStartBeat()
    {
        return m_notes[0].GetStartBeat();
    }

    public uint GetEndBeat()
    {
        Note lastNote = m_notes[m_notes.Count-1];
        return lastNote.GetStartBeat() + lastNote.GetLength();
    }

    public uint GetLinebreakBeat()
    {
        return m_linebreakBeat;
    }
}
