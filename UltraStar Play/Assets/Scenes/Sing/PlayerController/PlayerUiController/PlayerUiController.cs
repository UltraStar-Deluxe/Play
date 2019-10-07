using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiController : MonoBehaviour
{
    private SongMeta songMeta;
    private PlayerProfile playerProfile;

    private LineDisplayer lineDisplayer;
    private SentenceDisplayer sentenceDisplayer;
    private TotalScoreDisplayer totalScoreDisplayer;
    private SentenceRatingDisplayer sentenceRatingDisplayer;

    public void Init(SongMeta songMeta, Voice voice, PlayerProfile playerProfile)
    {
        this.songMeta = songMeta;
        this.playerProfile = playerProfile;

        lineDisplayer = GetComponentInChildren<LineDisplayer>();
        lineDisplayer.Init(6);

        sentenceDisplayer = GetComponentInChildren<SentenceDisplayer>();
        sentenceDisplayer.Init(12);

        totalScoreDisplayer = GetComponentInChildren<TotalScoreDisplayer>();

        sentenceRatingDisplayer = GetComponentInChildren<SentenceRatingDisplayer>();
    }

    public void DisplaySentence(Sentence currentSentence)
    {
        sentenceDisplayer.DisplaySentence(currentSentence);
    }

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        sentenceRatingDisplayer.ShowSentenceRating(sentenceRating);
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
