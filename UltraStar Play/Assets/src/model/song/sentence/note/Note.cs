using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    private readonly int m_pitch;
    private readonly uint m_startBeat;
    private readonly uint m_length;
    private readonly string m_text;
    private readonly ENoteType m_type;

    public Note(int pitch, uint startBeat, uint length, string text, ENoteType type)
    {
        m_pitch = pitch;
        m_startBeat = startBeat;
        m_length = length;
        m_text = text;
        m_type = type;
    }

    public uint GetLength()
    {
        return m_length;
    }

    public int GetPitch()
    {
        return m_pitch;
    }

    public uint GetStartBeat()
    {
        return m_startBeat;
    }

    public string GetText()
    {
        return m_text;
    }

    public new ENoteType GetType()
    {
        return m_type;
    }
}
