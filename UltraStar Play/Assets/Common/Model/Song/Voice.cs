using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class Voice : ISerializationCallbackReceiver
{
    public static readonly string soloVoiceName = "";
    public static readonly string firstVoiceName = "P1";
    public static readonly string secondVoiceName = "P2";

    public static readonly IComparer<Voice> comparerByName = new VoiceComparerByName();

    public string Name { get; private set; } = soloVoiceName;

    private readonly SentenceHashSet sentences = new SentenceHashSet();
    public IReadOnlyCollection<Sentence> Sentences { get { return sentences; } }

    public Voice()
    {
    }

    public Voice(string name)
    {
        SetName(name);
    }

    public Voice(IEnumerable<Sentence> sentences, string name)
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
        Name = name ?? throw new ArgumentNullException("Voice name cannot be null");
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
        Voice clone = new Voice(Name);
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
            return string.Compare(x.Name, y.Name, true, CultureInfo.InvariantCulture);
        }
    }
}
