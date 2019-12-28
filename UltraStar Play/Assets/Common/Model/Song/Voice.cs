using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Voice : ISerializationCallbackReceiver
{
    private readonly SentenceHashSet sentences = new SentenceHashSet();
    public IReadOnlyCollection<Sentence> Sentences { get { return sentences; } }

    public Voice()
    {
    }

    public Voice(List<Sentence> sentences)
    {
        SetSentences(sentences);
    }

    public void SetSentences(List<Sentence> sentences)
    {
        if (sentences == null)
        {
            throw new UnityException("Sentences cannot be null!");
        }
        this.sentences.Clear();
        foreach (Sentence sentence in sentences)
        {
            this.sentences.Add(sentence);
            sentence.SetVoice(this);
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
}
