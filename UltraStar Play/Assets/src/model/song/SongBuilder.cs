using System;
using System.Collections.Generic;
using UnityEngine;

public class SongBuilder
{
    // mutable versions of some objects
    private class MutableSentence
    {
        private readonly List<Note> m_notes = new List<Note>();

        public void AddNote(Note note)
        {
            m_notes.Add(note);
        }

        public Sentence AsSentence()
        {
            return new Sentence(m_notes, GetStartBeat(), GetEndBeat());
        }

        private uint GetStartBeat()
        {
            return m_notes[0].GetStartBeat();
        }

        private uint GetEndBeat()
        {
            Note lastNote = m_notes[m_notes.Count-1];
            return lastNote.GetStartBeat() + lastNote.GetLength();
        }
    }

    private class MutableVoice
    {
        private readonly string m_name;
        private readonly List<Sentence> m_sentences = new List<Sentence>();

        public MutableVoice(string name)
        {
            m_name = name;
        }

        public string GetName()
        {
            return m_name;
        }

        public void AddSentence(Sentence sentence)
        {
            m_sentences.Add(sentence);
        }

        public Voice AsVoice(){
            return new Voice(m_name, m_sentences);
        }
    }

    private class MutableSong
    {
        private readonly string m_path;
        private Dictionary<ESongHeader, System.Object> m_headers = new Dictionary<ESongHeader, System.Object>();
        private readonly Dictionary<string, MutableVoice> m_voices = new Dictionary<string, MutableVoice>(); // P# -> MutableVoice

        public MutableSong(string path)
        {
            m_path = path;
        }

        public void SetHeaders(Dictionary<ESongHeader, System.Object> headers)
        {
            m_headers = headers;
        }

        public Dictionary<ESongHeader, System.Object> GetHeaders()
        {
            return m_headers;
        }

        public void AddVoice(string identifier, string name)
        {
            m_voices.Add(identifier, new MutableVoice(name));
        }

        public void FixVoices()
        {
            // remove any DUETSINGERP voices for which a P voice also exists
            // then, change any remaining DUETSINGERP voice to a P voice
            List<string> voicesToRemove = new List<string>();
            List<string> voicesToRename = new List<string>();
            foreach (string identifier in m_voices.Keys)
            {
                if (identifier.StartsWith("DUETSINGERP", StringComparison.Ordinal))
                {
                    if (m_voices.ContainsKey(identifier.Substring(10)))
                    {
                        voicesToRemove.Add(identifier);
                    }
                    else
                    {
                        voicesToRename.Add(identifier);
                    }
                }
            }
            foreach (string identifier in voicesToRemove)
            {
                m_voices.Remove(identifier);
            }
            foreach (string identifier in voicesToRename)
            {
                string name = m_voices[identifier].GetName();
                m_voices.Remove(identifier);
                AddVoice(identifier.Substring(10), name);
            }
        }

        public int GetNumberOfVoices()
        {
            return m_voices.Count;
        }

        public MutableVoice GetVoice(string identifier)
        {
            return m_voices[identifier];
        }

        public Song AsSong()
        {
            List<Voice> voices = new List<Voice>();
            foreach (MutableVoice voice in m_voices.Values)
            {
                voices.Add(voice.AsVoice());
            }
            return new Song(m_headers, voices, m_path);
        }
    }

    private MutableVoice m_currentVoice;
    private MutableSentence m_currentSentence;
    private readonly MutableSong m_song;

    public SongBuilder(string path)
    {
        m_song = new MutableSong(path);
    }

    public void SaveCurrentSentence()
    {
        if (m_currentSentence == null)
        {
            return;
        }
        if (m_currentVoice == null)
        {
            if (m_song.GetNumberOfVoices() == 0)
            {
                // some default voice if it was never set, aka most solo songs
                m_song.AddVoice("_", "_");
                m_currentVoice = m_song.GetVoice("_");
            }
            else
            {
                throw new UnityException("One or more voices were named, you need to define who is singing right now");
            }
        }
        m_currentVoice.AddSentence(m_currentSentence.AsSentence());
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
