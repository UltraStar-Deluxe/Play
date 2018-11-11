using System;
using System.Collections.Generic;
using UnityEngine;

// this should be internal but tests become impossible
public class MutableVoice
{
    private readonly string m_name;
    private readonly List<MutableSentence> m_sentences = new List<MutableSentence>();

    public MutableVoice(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        m_name = name;
    }

    public string GetName()
    {
        return m_name;
    }

    public void AddSentence(MutableSentence sentence)
    {
        if (sentence == null)
        {
            throw new ArgumentNullException("sentence");
        }
        else if (sentence.IsEmpty())
        {
            Debug.LogWarning("Ignoring empty sentence (hint: it ends at linebreak: " + sentence.GetLinebreakBeat() + ")");
        }
        else
        {
            sentence.AssertValidNotes();
            m_sentences.Add(sentence);
        }
    }

    public Voice AsVoice()
    {
        AssertValidSentences();
        List<Sentence> sentences = new List<Sentence>();
        foreach (MutableSentence ms in m_sentences)
        {
            sentences.Add(new Sentence(ms.GetNotes(), ms.GetLinebreakBeat()));
        }
        return new Voice(m_name, sentences);
    }

    private void AssertValidSentences()
    {
        // assert that there are any sentences at all, do not silently drop a voice
        if (m_sentences.Count == 0)
        {
            throw new SongBuilderException("Voice '" + m_name + "' has no sentences");
        }
        // assert sorted sentences
        m_sentences.Sort((a, b) => a.GetStartBeat().CompareTo(b.GetStartBeat()));
        // try to do something sensible with the linebreaks
        for (int i = 0; i < m_sentences.Count - 1; i++)
        {
            uint linebreak = m_sentences[i].GetLinebreakBeat();
            uint endCurrent = m_sentences[i].GetEndBeat();
            uint startNext = m_sentences[i+1].GetStartBeat();
            
            if (linebreak == 0)
            {
                // if this exception is thrown, what probably happened is that the last sentence in the txt isn't actually the last sentence
                throw new SongBuilderException("A linebreak should be set for the sentence starting at beat " + m_sentences[i].GetStartBeat());
            }
            else if (endCurrent >= startNext)
            {
                throw new SongBuilderException("The sentences starting at beats " + m_sentences[i].GetStartBeat() + " and " + startNext + " are overlapping");
            }

            if (endCurrent < linebreak && linebreak <= startNext)
            {
                continue;
            }
            // the sentences do not overlap, but the linebreak is in the wrong place
            // set it at 1/3 between the sentences
            else
            {
                uint newLinebreak = endCurrent + 1 + ((startNext - (endCurrent+1)) / 3);
                m_sentences[i].SetLinebreakBeat(newLinebreak);
                Debug.LogWarning("The linebreak at beat " + linebreak + " conflicts with a sentence and was moved to " + newLinebreak);
            }
        }
    }
}
