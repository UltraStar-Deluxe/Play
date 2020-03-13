using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiController : MonoBehaviour
{
    [Inject]
    private Injector injector;

    private LineDisplayer lineDisplayer;
    private SentenceDisplayer sentenceDisplayer;
    private TotalScoreDisplayer totalScoreDisplayer;
    private SentenceRatingDisplayer sentenceRatingDisplayer;
    private BeatGridDisplayer beatGridDisplayer;
    private CurrentBeatGridDisplayer currentBeatGridDisplayer;

    public void Init(PlayerProfile playerProfile, MicProfile micProfile)
    {
        lineDisplayer = GetComponentInChildren<LineDisplayer>();
        lineDisplayer.UpdateLines(6);

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
            totalScoreDisplayer.SetColor(micProfile.Color);
            avatarImage.SetColor(micProfile.Color);
        }

        // Inject all children
        foreach (INeedInjection childThatNeedsInjection in GetComponentsInChildren<INeedInjection>())
        {
            injector.Inject(childThatNeedsInjection);
        }
    }

    public void DisplaySentence(Sentence currentSentence)
    {
        sentenceDisplayer.DisplaySentence(currentSentence);
        beatGridDisplayer?.DisplaySentence(currentSentence);
        currentBeatGridDisplayer?.DisplaySentence(currentSentence);
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
        sentenceDisplayer.CreatePerfectNoteEffect(perfectNote);
    }
}
