using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Takes the analyzed beats of the PlayerPitchTracker and calculates the player's score.
public class PlayerScoreControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // A total of 10000 points can be achieved.
    // Golden notes give double points.
    // Singing perfect lines gives a bonus of up to 1000 points.
    public static readonly int maxScore = 10000;
    public static readonly int maxPerfectSentenceBonusScore = 1000;
    public static readonly int maxScoreForNotes = maxScore - maxPerfectSentenceBonusScore;

    public int TotalScore => PlayerScore?.TotalScore ?? 0;

    [Inject]
    private PlayerPerformanceAssessmentControl playerPerformanceAssessmentControl;

    [Inject]
    private Voice voice;

    [Inject]
    private SingSceneMedleyControl medleyControl;

    [Inject]
    private PlayerProfile playerProfile;

    private readonly Subject<ScoreCalculatedEvent> scoreCalculatedEventStream = new();
    public IObservable<ScoreCalculatedEvent> ScoreCalculatedEventStream => scoreCalculatedEventStream;

    private readonly Subject<ScoreChangedEvent> scoreChangedEventStream = new();
    public IObservable<ScoreChangedEvent> ScoreChangedEventStream => scoreChangedEventStream;

    private ScoreCalculationData calculationData = new();
    public ISingingResultsPlayerScore CalculationData => calculationData;

    private ISingingResultsPlayerScore playerScore;
    public ISingingResultsPlayerScore PlayerScore
    {
        get => playerScore;
        set
        {
            int oldTotalScore = TotalScore;
            int newTotalScore = value.TotalScore;

            playerScore = value;

            if (oldTotalScore != newTotalScore)
            {
                FireScoreChangedEvent();
            }
        }
    }

    private readonly HashSet<int> processedBeats = new();
    private int firstBeatToScoreInclusive;

    private void Awake()
    {
        PlayerScore = calculationData;
    }

    public void OnInjectionFinished()
    {
        UpdateMaxScores(voice.Sentences);

        playerPerformanceAssessmentControl.NoteAssessedEventStream.Subscribe(evt => OnNoteAssessed(evt));
        playerPerformanceAssessmentControl.SentenceAssessedEventStream.Subscribe(evt => OnSentenceAssessed(evt));
        ScoreCalculatedEventStream
            // Fire changed in next frame such that others can manipulate the score if needed (e.g. online multiplayer).
            .DelayFrame(1)
            .Subscribe(_ => FireScoreChangedEvent());
    }

    private void OnNoteAssessed(PlayerPerformanceAssessmentControl.NoteAssessedEvent evt)
    {
        if (CommonOnlineMultiplayerUtils.IsRemotePlayerProfile(playerProfile))
        {
            // Calculate only the score of the own local player.
            return;
        }

        Note note = evt.Note;
        if (!medleyControl.IsNoteInMedleyRange(note))
        {
            return;
        }

        foreach (int correctlySungBeat in evt.CorrectlySungBeats)
        {
            ScoreCorrectlySungBeat(correctlySungBeat, note);
        }
    }

    private void ScoreCorrectlySungBeat(int beat, Note note)
    {
        if (beat < firstBeatToScoreInclusive)
        {
            return;
        }

        if (processedBeats.Contains(beat))
        {
            Debug.LogWarning($"Attempt to score beat multiple times: {beat}");
            return;
        }
        processedBeats.Add(beat);

        if (TotalScore >= maxScore)
        {
            return;
        }

        if (note.IsNormal)
        {
            calculationData.CorrectlySungNormalNoteLengthTotal++;
        }
        else if (note.IsGolden)
        {
            calculationData.CorrectlySungGoldenNoteLengthTotal++;
        }

        if (calculationData.HighestScoredBeat < beat)
        {
            calculationData.HighestScoredBeat = beat;
        }
    }

    private void OnSentenceAssessed(PlayerPerformanceAssessmentControl.SentenceAssessedEvent evt)
    {
        if (CommonOnlineMultiplayerUtils.IsRemotePlayerProfile(playerProfile))
        {
            // Calculate only the score of the own local player.
            return;
        }

        Sentence sentence = evt.Sentence;
        if (!medleyControl.IsSentenceInMedleyRange(sentence))
        {
            return;
        }

        int totalScorableNoteLength = sentence.Notes
                .Where(note => note.IsNormal || note.IsGolden)
                .Select(note => note.Length)
                .Sum();
        if (totalScorableNoteLength <= 0)
        {
            return;
        }

        if (evt.IsPerfect)
        {
            calculationData.PerfectSentenceCount++;
        }

        // Check score is within expected bounds
        int totalScoreWithoutMods = calculationData.TotalScore - calculationData.ModTotalScore;
        if (totalScoreWithoutMods > maxScore)
        {
            Debug.LogWarning($"Total score without mods is {totalScoreWithoutMods}, returning max score of {maxScore} instead. "
                             + $"(NormalNotesTotalScore: {calculationData.TotalScore}, GoldenNotesTotalScore: {calculationData.GoldenNotesTotalScore}, PerfectSentenceBonusTotalScore: {calculationData.PerfectSentenceBonusTotalScore}, "
                             + $"MaxScoreForNormalNotes: {calculationData.MaxScoreForNormalNotes}, MaxScoreForGoldenNotes: {calculationData.MaxScoreForGoldenNotes}, MaxScoreForNotes: {calculationData.MaxScoreForNotes}, "
                             + $"CorrectlySungNormalNoteLengthTotal: {calculationData.CorrectlySungNormalNoteLengthTotal}, CorrectlySungGoldenNoteLengthTotal: {calculationData.CorrectlySungGoldenNoteLengthTotal}, "
                             + $"NormalNoteLengthTotal {calculationData.NormalNoteLengthTotal}, GoldenNoteLengthTotal {calculationData.GoldenNoteLengthTotal})");
        }

        FireScoreCalculatedEvent();
    }

    private void UpdateMaxScores(IReadOnlyCollection<Sentence> sentences)
    {
        if (sentences.IsNullOrEmpty())
        {
            // Everything is zero
            calculationData = new();
            return;
        }

        // Calculate the points for a single beat of a normal or golden note
        calculationData.NormalNoteLengthTotal = 0;
        calculationData.GoldenNoteLengthTotal = 0;
        foreach (Sentence sentence in sentences)
        {
            calculationData.NormalNoteLengthTotal += GetNormalNoteLength(sentence);
            calculationData.GoldenNoteLengthTotal += GetGoldenNoteLength(sentence);
        }

        double scoreForCorrectBeatOfNormalNotes = maxScoreForNotes / ((double)calculationData.NormalNoteLengthTotal + (2 * calculationData.GoldenNoteLengthTotal));
        double scoreForCorrectBeatOfGoldenNotes = 2 * scoreForCorrectBeatOfNormalNotes;

        calculationData.MaxScoreForNormalNotes = scoreForCorrectBeatOfNormalNotes * calculationData.NormalNoteLengthTotal;
        calculationData.MaxScoreForGoldenNotes = scoreForCorrectBeatOfGoldenNotes * calculationData.GoldenNoteLengthTotal;

        // Countercheck: The sum of all points must be equal to MaxScoreForNotes
        double pointsForAllNotes = calculationData.MaxScoreForNormalNotes + calculationData.MaxScoreForGoldenNotes;
        bool isSound = Math.Abs(maxScoreForNotes - pointsForAllNotes) <= 0.01;
        if (!isSound)
        {
            Debug.LogWarning("The definition of scores for normal or golden notes is not sound: "
                + $"maxScoreForNormalNotes: {calculationData.MaxScoreForNormalNotes}, maxScoreForGoldenNotes: {calculationData.MaxScoreForGoldenNotes}, sum: {calculationData.MaxScoreForNormalNotes + calculationData.MaxScoreForGoldenNotes}");
        }

        // Round the values for the max score of normal / golden notes to avoid floating point inaccuracy.
        calculationData.MaxScoreForNormalNotes = Math.Ceiling(calculationData.MaxScoreForNormalNotes);
        calculationData.MaxScoreForGoldenNotes = Math.Ceiling(calculationData.MaxScoreForGoldenNotes);
        // The sum of the rounded points must not exceed the MaxScoreForNotes.
        // If the definition is sound then the overhang is at most 2 because of the above rounding.
        int overhang = (int)(calculationData.MaxScoreForNormalNotes + calculationData.MaxScoreForGoldenNotes) - maxScoreForNotes;
        calculationData.MaxScoreForNormalNotes -= overhang;

        // Remember the sentence count to calculate the points for a perfect sentence.
        calculationData.TotalSentenceCount = sentences.Count;
    }

    private int GetNormalNoteLength(Sentence sentence)
    {
        return sentence.Notes
            .Where(note => note.IsNormal && medleyControl.IsNoteInMedleyRange(note))
            .Select(note => note.Length)
            .Sum();
    }

    private int GetGoldenNoteLength(Sentence sentence)
    {
        return sentence.Notes
            .Where(note => note.IsGolden && medleyControl.IsNoteInMedleyRange(note))
            .Select(note => note.Length)
            .Sum();
    }

    public void SkipToBeat(int beat)
    {
        if (beat >= firstBeatToScoreInclusive)
        {
            firstBeatToScoreInclusive = beat;
        }
    }

    public void SetCalculationData(ISingingResultsPlayerScore score)
    {
        if (score is not ScoreCalculationData newScorecalculationData)
        {
            Debug.LogWarning($"Attempt to set incompatible score calculation data: actual: '{score}', expected: {typeof(ScoreCalculationData)}");
            return;
        }

        calculationData = newScorecalculationData;

        if (calculationData.HighestScoredBeat > 0)
        {
            SkipToBeat(calculationData.HighestScoredBeat);
            Debug.Log($"Skipped to beat {calculationData.HighestScoredBeat} because it was already scored.");
        }
    }

    public void SetModTotalScore(int newModTotalScore, bool sendEvent = false)
    {
        calculationData.ModTotalScore = newModTotalScore;
        if (sendEvent)
        {
            FireScoreCalculatedEvent();
        }
    }

    private void FireScoreCalculatedEvent()
    {
        scoreCalculatedEventStream.OnNext(new ScoreCalculatedEvent(calculationData.TotalScore));
    }

    private void FireScoreChangedEvent()
    {
        scoreChangedEventStream.OnNext(new ScoreChangedEvent(TotalScore));
    }

    public class ScoreChangedEvent
    {
        public int TotalScore { get; private set; }

        public ScoreChangedEvent(int totalScore)
        {
            TotalScore = totalScore;
        }
    }

    public class ScoreCalculatedEvent
    {
        public int TotalScore { get; private set; }

        public ScoreCalculatedEvent(int totalScore)
        {
            TotalScore = totalScore;
        }
    }

    private class ScoreCalculationData : ISingingResultsPlayerScore
    {
        public int HighestScoredBeat { get; set; }

        public double MaxScoreForNormalNotes { get; set; }
        public double MaxScoreForGoldenNotes { get; set; }
        public double MaxScoreForNotes => MaxScoreForNormalNotes + MaxScoreForGoldenNotes;

        public int NormalNoteLengthTotal { get; set; }
        public int CorrectlySungNormalNoteLengthTotal { get; set; }

        public int GoldenNoteLengthTotal { get; set; }
        public int CorrectlySungGoldenNoteLengthTotal { get; set; }

        public int TotalSentenceCount { get; set; }
        public int PerfectSentenceCount { get; set; }

        public int NormalNotesTotalScore
        {
            get
            {
                if (CorrectlySungNormalNoteLengthTotal <= 0
                    || NormalNoteLengthTotal <= 0)
                {
                    return 0;
                }
                return (int)(MaxScoreForNormalNotes * CorrectlySungNormalNoteLengthTotal / NormalNoteLengthTotal);
            }
        }

        public int GoldenNotesTotalScore
        {
            get
            {
                if (CorrectlySungGoldenNoteLengthTotal <= 0
                    || GoldenNoteLengthTotal <= 0)
                {
                    return 0;
                }
                return (int)(MaxScoreForGoldenNotes * CorrectlySungGoldenNoteLengthTotal / GoldenNoteLengthTotal);
            }
        }

        public int PerfectSentenceBonusTotalScore
        {
            get
            {
                int targetSentenceCount = TotalSentenceCount > 20
                    ? 20
                    : TotalSentenceCount;
                if (targetSentenceCount <= 0)
                {
                    return 0;
                }
                double score = (double)maxPerfectSentenceBonusScore * PerfectSentenceCount / targetSentenceCount;

                // Round the score up
                score = Math.Ceiling(score);
                if (score > maxPerfectSentenceBonusScore)
                {
                    score = maxPerfectSentenceBonusScore;
                }
                return (int)score;
            }
        }

        public int ModTotalScore { get; set; }

        public int TotalScore => NormalNotesTotalScore
                                 + GoldenNotesTotalScore
                                 + PerfectSentenceBonusTotalScore
                                 + ModTotalScore;
    }
}
