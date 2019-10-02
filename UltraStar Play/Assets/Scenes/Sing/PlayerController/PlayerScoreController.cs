using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerScoreController : MonoBehaviour
{
    public const int MaxScore = 10000;

    public int Score { get; private set; }

    private Dictionary<Sentence, int> sentenceToMaxScoreMap = new Dictionary<Sentence, int>();

    public void Init(Voice voice)
    {
        UpdateMaxScoresForSentences(voice.Sentences);
    }

    public int CalculateScoreForSentence(Sentence sentence, RecordedSentence recordedSentence)
    {
        if (recordedSentence == null)
        {
            return 0;
        }
        int sentenceNoteLength = sentence.Notes.Select(note => (int)note.Length).Sum();
        int correctNormalNoteLength = GetCorrectlySungNormalNoteLength(sentence, recordedSentence);
        int correctGoldenNoteLength = GetCorrectlySungGoldenNoteLength(sentence, recordedSentence);
        int correctNoteLengthTotal = correctNormalNoteLength + correctGoldenNoteLength;
        int sentenceMaxScore = sentenceToMaxScoreMap[sentence];
        int scoreForSentence = sentenceMaxScore * correctNoteLengthTotal / sentenceNoteLength;
        return scoreForSentence;
    }

    private int GetCorrectlySungGoldenNoteLength(Sentence sentence, RecordedSentence recordedSentence)
    {
        return 0;
    }

    private int GetCorrectlySungNormalNoteLength(Sentence sentence, RecordedSentence recordedSentence)
    {
        return 0;
    }

    private void UpdateMaxScoresForSentences(List<Sentence> sentences)
    {
        // Calculate max score of each sentence.
        sentenceToMaxScoreMap.Clear();

        // TODO: Use real calculation for max sentence score
        // For now just use a direct mapping of the percentage of the sentence in the song to its corresponding score
        int totalNoteLength = sentences.SelectMany(sentence => sentence.Notes).Select(note => (int)note.Length).Sum();
        if (totalNoteLength == 0)
        {
            return;
        }
        foreach (Sentence sentence in sentences)
        {
            int sentenceNoteLength = sentence.Notes.Select(note => (int)note.Length).Sum();
            int sentenceMaxScore = MaxScore * sentenceNoteLength / totalNoteLength;
            sentenceToMaxScoreMap.Add(sentence, sentenceMaxScore);
        }
    }

    public SentenceRating GetSentenceRating(Sentence currentSentence, int scoreForSentence)
    {
        int sentenceMaxScore = sentenceToMaxScoreMap[currentSentence];
        if (sentenceMaxScore <= 0)
        {
            return null;
        }
        double percentage = scoreForSentence / sentenceMaxScore;
        foreach (SentenceRating sentenceRating in SentenceRating.Values)
        {
            if (percentage >= sentenceRating.PercentageThreshold)
            {
                return sentenceRating;
            }
        }
        return null;
    }
}
