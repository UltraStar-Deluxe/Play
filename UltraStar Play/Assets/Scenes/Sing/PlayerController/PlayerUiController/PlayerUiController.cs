using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiController : MonoBehaviour
{
    private SongMeta songMeta;
    private PlayerProfile playerProfile;

    private SentenceDisplayer sentenceDisplayer;
    private TotalScoreDisplayer totalScoreDisplayer;
    private SentenceRatingDisplayer sentenceRatingDisplayer;

    public void Init(SongMeta songMeta, Voice voice, PlayerProfile playerProfile)
    {
        this.songMeta = songMeta;
        this.playerProfile = playerProfile;

        sentenceDisplayer = GetComponentInChildren<SentenceDisplayer>();
        totalScoreDisplayer = GetComponentInChildren<TotalScoreDisplayer>();
        sentenceRatingDisplayer = GetComponentInChildren<SentenceRatingDisplayer>();
    }

    public void SetCurrentSentence(Sentence currentSentence)
    {
        sentenceDisplayer.DisplayNotes(currentSentence);
        sentenceDisplayer.DisplayRecordedNotes(null);
    }

    public void ShowSentenceRating(SentenceRating sentenceRating, int scoreForSentence)
    {
        sentenceRatingDisplayer.ShowSentenceRating(sentenceRating, scoreForSentence);
    }

    public void ShowTotalScore(int score)
    {
        totalScoreDisplayer.ShowTotalScore(score);
    }

    public void DisplayRecordedNotes(List<RecordedNote> recordedNotes)
    {
        sentenceDisplayer.DisplayRecordedNotes(recordedNotes);
    }
}
