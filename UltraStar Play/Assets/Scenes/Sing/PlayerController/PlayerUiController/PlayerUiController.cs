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
    private BeatGridDisplayer beatGridDisplayer;
    private CurrentBeatGridDisplayer currentBeatGridDisplayer;

    public void Init(PlayerProfile playerProfile, MicProfile micProfile)
    {
        lineDisplayer = GetComponentInChildren<LineDisplayer>();
        lineDisplayer.Init(6);

        sentenceDisplayer = GetComponentInChildren<SentenceDisplayer>();
        sentenceDisplayer.Init(12, micProfile);

        totalScoreDisplayer = GetComponentInChildren<TotalScoreDisplayer>();

        sentenceRatingDisplayer = GetComponentInChildren<SentenceRatingDisplayer>();

        beatGridDisplayer = GetComponentInChildren<BeatGridDisplayer>();

        currentBeatGridDisplayer = GetComponentInChildren<CurrentBeatGridDisplayer>();

        PlayerNameText playerNameText = GetComponentInChildren<PlayerNameText>();
        playerNameText.SetPlayerProfile(playerProfile);

        AvatarImage avatarImage = GetComponentInChildren<AvatarImage>();
        avatarImage.SetPlayerProfile(playerProfile);

        if (micProfile != null)
        {
            totalScoreDisplayer.SetColorOfMicProfile(micProfile);
            avatarImage.SetColorOfMicProfile(micProfile);
        }
    }

    public void DisplaySentence(Sentence currentSentence)
    {
        sentenceDisplayer.DisplaySentence(currentSentence);
        beatGridDisplayer?.DisplaySentence(currentSentence);
        currentBeatGridDisplayer?.DisplaySentence(currentSentence);
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
