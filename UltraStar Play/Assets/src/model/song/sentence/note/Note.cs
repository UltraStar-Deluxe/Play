using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    public readonly int m_pitch;
    public readonly int m_startBeat;
    public readonly int m_length;
    public readonly string m_text;
    public readonly ENoteType m_type;

    public Note(int pitch, int startBeat, int length, string text, ENoteType type)
    {
        m_pitch = pitch;
        m_startBeat = startBeat;
        m_length = length;
        m_text = text;
        m_type = type;
    }
}
