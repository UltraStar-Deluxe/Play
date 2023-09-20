using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class Voice
{
    public static readonly string soloVoiceId = "";
    public static readonly string firstVoiceId = EVoiceId.P1.ToString();
    public static readonly string secondVoiceId = EVoiceId.P2.ToString();
    public static readonly string mergedVoiceId = "MERGED";

    public static readonly IComparer<Voice> comparerById = new VoiceComparerById();

    public string Id { get; private set; } = soloVoiceId;

    private readonly HashSet<Sentence> sentences = new();
    public IReadOnlyCollection<Sentence> Sentences { get { return sentences; } }

    public Voice() : this(soloVoiceId)
    {
    }

    public Voice(string id)
    {
        SetId(id);
    }

    public Voice(string id, IEnumerable<Sentence> sentences) : this(id)
    {
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

    public void SetId(string newId)
    {
        Id = newId ?? throw new ArgumentNullException(nameof(newId));
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
        Voice clone = new(Id);
        foreach (Sentence sentence in Sentences)
        {
            Sentence sentenceCopy = sentence.CloneDeep();
            clone.AddSentence(sentenceCopy);
        }
        return clone;
    }

    public class VoiceComparerById : IComparer<Voice>
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
            return string.Compare(x.Id, y.Id, true, CultureInfo.InvariantCulture);
        }
    }

    public static bool VoiceIdEquals(string a, string b)
    {
        return a == b
               || (a.IsNullOrEmpty() && b.IsNullOrEmpty())
               || (a == firstVoiceId && b == soloVoiceId)
               || (a == soloVoiceId && b == firstVoiceId);
    }

    public static string NormalizeVoiceId(string voiceId)
    {
        return voiceId.IsNullOrEmpty() || voiceId == soloVoiceId
            ? firstVoiceId
            : voiceId;
    }

    public static string GetNextVoiceId(string currentVoiceId)
    {
        if (currentVoiceId == firstVoiceId)
        {
            return secondVoiceId;
        }
        else if (currentVoiceId == secondVoiceId)
        {
            return mergedVoiceId;
        }
        else
        {
            return firstVoiceId;
        }
    }
}
