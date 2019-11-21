using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerUiController : MonoBehaviour
{
    private LineDisplayer lineDisplayer;
    private SentenceDisplayer sentenceDisplayer;
    private TotalScoreDisplayer totalScoreDisplayer;
    private SentenceRatingDisplayer sentenceRatingDisplayer;

    public void Init()
    {
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

    public void RemoveAllDisplayedNotes()
    {
        sentenceDisplayer.RemoveAllDisplayedNotes();
    }

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        sentenceRatingDisplayer.ShowSentenceRating(sentenceRating);
    }

    public void ShowTotalScore(int score)
    {
        totalScoreDisplayer.ShowTotalScore(score);
    }

    public void DisplayRecordedNote(RecordedNote recordedNote)
    {
        sentenceDisplayer.DisplayRecordedNote(recordedNote);
    }

    public void CreatePerfectSentenceEffect()
    {
        sentenceDisplayer.CreatePerfectSentenceEffect();
    }

    public void CreatePerfectNoteEffect(Note perfectNote)
    {
        UiNote uiNote = GetComponentsInChildren<UiNote>().Where(it => it.Note == perfectNote).FirstOrDefault();
        if (uiNote != null)
        {
            uiNote.CreatePerfectNoteEffect();
        }
    }
}
