using System.Collections.Generic;
using UnityEngine;

public class Voice
{
    private readonly string m_name;
    private readonly List<Sentence> m_sentences;
    
    public Voice(string name, List<Sentence> sentences)
    {
        if (name == null || name.Length < 1)
        {
            throw new UnityException("name is null or empty!");
        }
        m_name = name;
        if (sentences == null || sentences.Count < 1)
        {
            throw new UnityException("sentences is null or empty!");
        }
        m_sentences = sentences;
    }

    public string GetName()
    {
        return m_name;
    }

    public List<Sentence> GetSentences()
    {
        return m_sentences;
    }
}
