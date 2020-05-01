using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiController : MonoBehaviour
{
    [Inject]
    private PlayerScoreController playerScoreController;

    private LineDisplayer lineDisplayer;
    private SentenceDisplayer sentenceDisplayer;
    private TotalScoreDisplayer totalScoreDisplayer;
    private SentenceRatingDisplayer sentenceRatingDisplayer;
    private BeatGridDisplayer beatGridDisplayer;
    private CurrentBeatGridDisplayer currentBeatGridDisplayer;

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

    public void Init(PlayerProfile playerProfile, MicProfile micProfile)
    {
        lineDisplayer = GetComponentInChildren<LineDisplayer>();
        lineDisplayer.UpdateLines(lineCount);

        sentenceDisplayer = GetComponentInChildren<SentenceDisplayer>();
        sentenceDisplayer.Init(lineCount * 2, micProfile);

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
