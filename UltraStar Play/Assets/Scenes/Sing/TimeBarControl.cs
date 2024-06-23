using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeBarControl : INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.innerTimeBarSentenceEntryContainer)]
    private VisualElement innerTimeBarSentenceEntryContainer;

    [Inject(UxmlName = R.UxmlNames.timeBarPositionIndicator)]
    private VisualElement timeBarPositionIndicator;

    [Inject(UxmlName = R.UxmlNames.timeValueLabel)]
    private Label timeValueLabel;

    [Inject]
    private SingSceneMedleyControl medleyControl;

    [Inject]
    private SongMeta songMeta;

    private double LateStartInSongInMillis => songMeta?.StartInMillis ?? 0;
    private double EarlyEndInSongInMillis => songMeta?.EndInMillis ?? 0;

    public void UpdateTimeValueLabel(double positionInMillis, double durationInMillis)
    {
        if (positionInMillis < 0
            || durationInMillis <= 0)
        {
            timeValueLabel.HideByVisibility();
            return;
        }
        timeValueLabel.ShowByVisibility();

        double positionConsideringStartTag = positionInMillis - LateStartInSongInMillis;
        double durationInMillisConsideringEndTag = EarlyEndInSongInMillis > 0
            ? Math.Min(durationInMillis, EarlyEndInSongInMillis)
            : durationInMillis;
        double durationInMillisConsideringStartAndEndTag = durationInMillisConsideringEndTag - LateStartInSongInMillis;

        double remainingTimeInSeconds = (durationInMillisConsideringStartAndEndTag - positionConsideringStartTag) / 1000;
        if (remainingTimeInSeconds < 0)
        {
            timeValueLabel.SetTranslatedText(Translation.Of("00:00"));
            return;
        }

        int mins = (int)Math.Floor(remainingTimeInSeconds / 60);
        string minsPadding = (mins < 10) ? "0" : "";
        int secs = (int)Math.Floor(remainingTimeInSeconds % 60);
        string secsPadding = (secs < 10) ? "0" : "";
        timeValueLabel.SetTranslatedText(Translation.Of($"{minsPadding}{mins}:{secsPadding}{secs}"));
    }

    public void UpdatePositionIndicator(double positionInMillis, double durationInMillis)
    {
        double positionConsideringStartTag = positionInMillis - LateStartInSongInMillis;
        double durationInMillisConsideringEndTag = EarlyEndInSongInMillis > 0
            ? Math.Min(durationInMillis, EarlyEndInSongInMillis)
            : durationInMillis;
        double durationInMillisConsideringStartAndEndTag = durationInMillisConsideringEndTag - LateStartInSongInMillis;

        float positionInPercent = (float)(100 * positionConsideringStartTag / durationInMillisConsideringStartAndEndTag);
        timeBarPositionIndicator.style.width = new StyleLength(new Length(positionInPercent, LengthUnit.Percent));
    }

    public void UpdateTimeBarRectangles(SongMeta songMeta, List<PlayerControl> playerControls, double durationInMillis)
    {
        innerTimeBarSentenceEntryContainer.Clear();

        if (durationInMillis <= 0)
        {
            return;
        }

        int playerControlIndex = 0;
        foreach (PlayerControl playerControl in playerControls)
        {
            CreateRectangles(songMeta, playerControl, durationInMillis, playerControlIndex, playerControls.Count);
            playerControlIndex++;
        }
    }

    private void CreateRectangles(SongMeta songMeta, PlayerControl playerControl, double durationInMillis, int playerIndex, int playerCount)
    {
        foreach (Sentence sentence in playerControl.Voice.Sentences)
        {
            if (!medleyControl.IsSentenceInMedleyRange(sentence))
            {
                continue;
            }

            double startPosInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, sentence.MinBeat);
            double endPosInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, sentence.MaxBeat);

            if (playerCount <= 3)
            {
                // Show individual rectangles for each player
                float heightPercent = playerCount > 0 ? (100 / playerCount) : 100;
                float topPercent = playerIndex * heightPercent;
                MicProfile micProfile = playerControl.MicProfile;
                CreateRectangle(micProfile, startPosInMillis, endPosInMillis, durationInMillis, topPercent, heightPercent);
            }
            else
            {
                // Just show where the lyrics are, independent of the concrete player
                CreateRectangle(null, startPosInMillis, endPosInMillis, durationInMillis, 0, 100);
            }
        }
    }

    private void CreateRectangle(MicProfile micProfile, double startPosInMillis, double endPosInMillis, double durationInMillis, float topPercent, float heightPercent)
    {
        double durationInMillisConsideringEndTag = EarlyEndInSongInMillis > 0
            ? Math.Min(durationInMillis, EarlyEndInSongInMillis)
            : durationInMillis;
        double durationInMillisConsideringStartAndEndTag = durationInMillisConsideringEndTag - LateStartInSongInMillis;
        double startPosInMillisConsideringStartTag = startPosInMillis - LateStartInSongInMillis;
        double endPosInMillisConsideringStartTag = endPosInMillis - LateStartInSongInMillis;

        float startPosPercentage = (float)(100 * startPosInMillisConsideringStartTag / durationInMillisConsideringStartAndEndTag);
        float endPosPercentage = (float)(100 * endPosInMillisConsideringStartTag / durationInMillisConsideringStartAndEndTag);

        if (endPosPercentage < 0
            || startPosPercentage > 100)
        {
            // Outside of visible area
            return;
        }

        // Fit into parent
        NumberUtils.Limit(startPosPercentage, 0, 100);
        NumberUtils.Limit(endPosPercentage, 0, 100);

        VisualElement rectangle = new();
        rectangle.style.position = new StyleEnum<Position>(Position.Absolute);
        rectangle.style.left = new StyleLength(new Length(startPosPercentage, LengthUnit.Percent));
        rectangle.style.width = new StyleLength(new Length(endPosPercentage - startPosPercentage, LengthUnit.Percent));
        rectangle.style.top = new StyleLength(new Length(topPercent, LengthUnit.Percent));
        rectangle.style.height = new StyleLength(new Length(heightPercent, LengthUnit.Percent));

        // Set color of rectangle to color of mic.
        if (micProfile != null)
        {
            rectangle.style.backgroundColor = new StyleColor(micProfile.Color);
        }
        else
        {
            rectangle.style.backgroundColor = new StyleColor(Color.grey);
        }
        innerTimeBarSentenceEntryContainer.Add(rectangle);
    }
}
