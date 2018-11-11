using System;
using System.Collections.Generic;
using UnityEngine;

public class SongBuilder
{
    private MutableVoice m_currentVoice;
    private MutableSentence m_currentSentence;
    private readonly MutableSong m_song;

    public SongBuilder(string path)
    {
        m_song = new MutableSong(path);
    }

    public void SetLinebreakBeat(uint beat)
    {
        if (m_currentSentence == null)
        {
            throw new SongBuilderException("Cannot set linebreak at beat " + beat);
        }
        m_currentSentence.SetLinebreakBeat(beat);
    }

    public void SaveCurrentSentence()
    {
        if (m_currentSentence == null)
        {
            return;
        }
        if (m_currentVoice == null)
        {
            switch (m_song.GetNumberOfVoices())
            {
                case 0:
                    // some default voice if it was never set, aka most solo songs
                    m_song.AddVoice("_", "_");
                    m_currentVoice = m_song.GetVoice("_");
                    break;
                case 1:
                    // solo song with named voice
                    m_currentVoice = m_song.GetVoice(m_song.GetVoiceNames()[0]);
                    break;
                default:
                    throw new SongBuilderException("Multiple voices were named, you need to define who is singing right now");
            }
        }
        m_currentVoice.AddSentence(m_currentSentence);
        m_currentSentence = null;
    }

    public void SetCurrentVoice(string identifier)
    {
        m_currentVoice = m_song.GetVoice(identifier);
    }

    public void AddVoice(string identifier, string name)
    {
        m_song.AddVoice(identifier, name);
    }

    public void FixVoices()
    {
        m_song.FixVoices();
    }

    public void AddNote(Note note)
    {
        if (m_currentSentence == null)
        {
            m_currentSentence = new MutableSentence();
        }
        m_currentSentence.AddNote(note);
    }

    public void SetSongHeaders(Dictionary<ESongHeader, System.Object> headers)
    {
        m_song.SetHeaders(headers);
    }

    public Dictionary<ESongHeader, System.Object> GetSongHeaders()
    {
        return m_song.GetHeaders();
    }

    public Song AsSong()
    {
        return m_song.AsSong();
    }
}
