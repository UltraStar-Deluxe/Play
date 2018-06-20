using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

public class Song
{
    private readonly string m_path;
    private readonly string m_folderPath;
    private readonly List<List<Sentence>> m_voicesSentences;
    private readonly Dictionary<ESongHeader, System.Object> m_headers;

    public Song(Dictionary<ESongHeader, System.Object> headers, List<List<Sentence>> voicesSentences, string path)
    {
        if(headers == null || headers.Count == 0)
        {
            throw new UnityException("headers is null or empty! Can not initialize Song.");
        }
        m_headers = headers;

        if (voicesSentences == null || voicesSentences.Count == 0 || voicesSentences[0] == null)// || playerSentences[0].Count == 0)
        {
            throw new UnityException("playerSentences is null or empty! Can not initialize Song.");
        }
        m_voicesSentences = voicesSentences;

        if(path == null || path.Length < 6 || !File.Exists(path))
        {
            throw new UnityException("Invalid song file path. Can not create song.");
        }
        m_path = new FileInfo(path).FullName;
        m_folderPath = new FileInfo(path).Directory.FullName;
    }

    public ReadOnlyCollection<Sentence> GetSentences(int voiceNr)
    {
        if(voiceNr > (m_voicesSentences.Count -1))
        {
            throw new UnityException("Invalid voiceNumber. Can not get sentences for that player!");
        }
        return m_voicesSentences[voiceNr].AsReadOnly();
    }

    public string GetStringHeaderOrNull(ESongHeader key)
    {
        object value;
        m_headers.TryGetValue(key, out value);
        return (string)value;
    }

    public int GetIntHeaderOrNull(ESongHeader key)
    {
        object value;
        m_headers.TryGetValue(key, out value);
        return (int)value;
    }

    public float GetFloatHeaderOrNull(ESongHeader key)
    {
        object value;
        m_headers.TryGetValue(key, out value);
        return (float)value;
    }

    public string GetFilePath()
    {
        return m_path;
    }

    public string GetFolderPath()
    {
        return m_folderPath;
    }
    public bool IsDuet()
    {
        return (m_voicesSentences.Count == 2 || m_voicesSentences.Count == 3);
    }
}