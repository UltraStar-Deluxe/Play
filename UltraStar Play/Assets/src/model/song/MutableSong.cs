using System;
using System.Collections.Generic;

// this should be internal but tests become impossible
public class MutableSong
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

    public List<string> GetVoiceNames()
    {
        List<string> names = new List<string>();
        foreach (string name in m_voices.Keys)
        {
            names.Add(name);
        }
        return names;
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
