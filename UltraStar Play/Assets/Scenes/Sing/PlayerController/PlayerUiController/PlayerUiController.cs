using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiController : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection, IInjectionFinishedListener
{
    [Inject]
    private PlayerScoreController playerScoreController;

    [Inject]
    private PlayerController playerController;

    [Inject]
    private MicProfile micProfile;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private LineDisplayer lineDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private SentenceDisplayer sentenceDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private TotalScoreDisplayer totalScoreDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private SentenceRatingDisplayer sentenceRatingDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren, optional = true)]
    private BeatGridDisplayer beatGridDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren, optional = true)]
    private CurrentBeatGridDisplayer currentBeatGridDisplayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private PlayerNameText playerNameText;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private AvatarImage avatarImage;

    public int lineCount = 10;

    void Start()
    {
        // Show rating and score after each sentence
        playerScoreController.SentenceScoreEventStream.Subscribe(sentenceScoreEvent =>
        {
            ShowTotalScore(playerScoreController.TotalScore);
            ShowSentenceRating(sentenceScoreEvent.SentenceRating);
        });

        // Show an effect for perfectly sung notes
        playerScoreController.NoteScoreEventStream.Subscribe(noteScoreEvent =>
        {
            if (noteScoreEvent.NoteScore.IsPerfect)
            {
                CreatePerfectNoteEffect(noteScoreEvent.NoteScore.Note);
            }
        });

        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        playerScoreController.SentenceScoreEventStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.AllMatch(x => x.SentenceRating == SentenceRating.Perfect))
            // Create an effect for these.
            .Subscribe(xs => CreatePerfectSentenceEffect());
    }

    public void OnInjectionFinished()
    {
        lineDisplayer.UpdateLines(lineCount);
        sentenceDisplayer.Init(lineCount * 2, micProfile);
        playerNameText.SetPlayerProfile(playerProfile);
        avatarImage.SetPlayerProfile(playerProfile);

        if (micProfile != null)
        {
            totalScoreDisplayer.SetColor(micProfile.Color);
            avatarImage.SetColor(micProfile.Color);
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
