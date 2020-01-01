using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Voice : ISerializationCallbackReceiver
{
    public static readonly IComparer<Voice> comparerByName = new VoiceComparerByName();

    public string Name { get; private set; }

    private readonly SentenceHashSet sentences = new SentenceHashSet();
    public IReadOnlyCollection<Sentence> Sentences { get { return sentences; } }

    public Voice()
    {
    }

    public Voice(List<Sentence> sentences, string name)
    {
        SetName(name);
        SetSentences(sentences);
    }

    public void SetSentences(IEnumerable<Sentence> newSentences)
    {
        if (newSentences == null)
        {
            throw new UnityException("Sentences cannot be null!");
        }

        foreach (Sentence sentence in new List<Sentence>(sentences))
        {
            RemoveSentence(sentence);
        }
        foreach (Sentence sentence in newSentences)
        {
            AddSentence(sentence);
        }
    }

    public void AddSentence(Sentence sentence)
    {
        if (sentence == null)
        {
            throw new UnityException("Sentence cannot be null");
        }

        // The check is needed to avoid recursive loop between Voice.AddSentence and Sentence.SetVoice.
        if (sentences.Contains(sentence))
        {
            return;
        }
        sentences.Add(sentence);
        sentence.SetVoice(this);
    }

    public void RemoveSentence(Sentence sentence)
    {
        if (sentence == null)
        {
            throw new UnityException("Sentence cannot be null");
        }

        // The check is needed to avoid recursive loop between Voice.RemoveSentence and Sentence.SetVoice.
        if (!sentences.Contains(sentence))
        {
            return;
        }
        sentences.Remove(sentence);
        sentence.SetVoice(null);
    }

    public void SetName(string name)
    {
        this.Name = name;
    }

    public void OnBeforeSerialize()
    {
        // Do nothing. Implementation of ISerializationCallbackReceiver
    }

    public void OnAfterDeserialize()
    {
        foreach (Sentence sentence in sentences)
        {
            sentence.SetVoice(this);
        }
    }

    public Voice CloneDeep()
    {
        Voice clone = new Voice();
        clone.SetName(Name);
        foreach (Sentence sentence in Sentences)
        {
            Sentence sentenceCopy = sentence.CloneDeep();
            clone.AddSentence(sentenceCopy);
        }
        return clone;
    }

    public class VoiceComparerByName : IComparer<Voice>
    {
        public int Compare(Voice x, Voice y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }
            return x.Name.CompareTo(y.Name);
        }
    }
}
