using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VoicesMerger
{
    public static Voice Merge(List<Voice> voices)
    {
        if (voices.IsNullOrEmpty())
        {
            return null;
        }

        if (voices.Count == 1)
        {
            return voices.FirstOrDefault();
        }

        MergedVoice mergedVoice = new(voices);
        foreach (Voice voice in voices.ToList())
        {
            foreach (Sentence newSentence in voice.Sentences.ToList())
            {
                // Add the sentence if there is none yet.
                Sentence overlappingSentence = mergedVoice.Sentences
                    .FirstOrDefault(existingSentence => SongMetaUtils.IsBeatInSentence(existingSentence, newSentence.MinBeat, true, false)
                                                        || SongMetaUtils.IsBeatInSentence(existingSentence, newSentence.MaxBeat, true, false));
                if (overlappingSentence != null)
                {
                    Debug.Log($"{newSentence} overlaps with {overlappingSentence}");
                }

                if (overlappingSentence == null)
                {
                    Sentence newSentenceClone = newSentence.CloneDeep();
                    mergedVoice.AddSentence(newSentenceClone);
                }
            }
        }

        // Minimize sentences to make sure that they do not overlap
        foreach (Sentence mergedSentence in mergedVoice.Sentences)
        {
            mergedSentence.SetLinebreakBeat(0);
        }

        // Sort sentences
        List<Sentence> sortedSentences = mergedVoice.Sentences.ToList();
        sortedSentences.Sort(Sentence.comparerByStartBeat);
        mergedVoice.SetSentences(sortedSentences);

        return mergedVoice;
    }

    private class MergedVoice : Voice
    {
        public List<Voice> OriginalVoices { get; private set; }

        public MergedVoice(List<Voice> originalVoices)
        {
            OriginalVoices = originalVoices;
        }
    }
}
