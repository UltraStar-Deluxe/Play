using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentence
{
    private readonly List<Note> m_notes;
    private readonly uint m_startBeat;
    private readonly uint m_endBeat;

    public Sentence(List<Note> notes, uint startBeat, uint endBeat)
    {
        if (notes == null || notes.Count < 1)
        {
            throw new UnityException("notes is null or empty!");
        }
        m_notes = notes;
        m_startBeat = startBeat;
        m_endBeat = endBeat;
    }

    public List<Note> GetNotes()
    {
        return m_notes;
    }

    public uint GetStartBeat()
    {
        return m_startBeat;
    }

    public uint GetEndBeat()
    {
        return m_endBeat;
    }
}
