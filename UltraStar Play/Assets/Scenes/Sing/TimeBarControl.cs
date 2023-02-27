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

    public void UpdateTimeValueLabel(double positionInSongInMillis, double durationOfSongInMillis)
    {
        double remainingTimeInSeconds = (durationOfSongInMillis - positionInSongInMillis) / 1000;
        int mins = (int)Math.Floor(remainingTimeInSeconds / 60);
        string minsPadding = (mins < 10) ? "0" : "";
        int secs = (int)Math.Floor(remainingTimeInSeconds % 60);
        string secsPadding = (secs < 10) ? "0" : "";
        timeValueLabel.text = $"{minsPadding}{mins}:{secsPadding}{secs}";
    }

    public void UpdatePositionIndicator(double positionInSongInMillis, double durationOfSongInMillis)
    {
        float positionInPercent = (float)(100 * positionInSongInMillis / durationOfSongInMillis);
        timeBarPositionIndicator.style.left = new StyleLength(new Length(positionInPercent, LengthUnit.Percent));
    }

    public void UpdateTimeBarRectangles(SongMeta songMeta, List<PlayerControl> playerControls, double durationOfSongInMillis)
    {
        innerTimeBarSentenceEntryContainer.Clear();
        int playerControlIndex = 0;
        foreach (PlayerControl playerControl in playerControls)
        {
            CreateRectangles(songMeta, playerControl, durationOfSongInMillis, playerControlIndex, playerControls.Count);
            playerControlIndex++;
        }
    }

    private void CreateRectangles(SongMeta songMeta, PlayerControl playerControl, double durationOfSongInMillis, int playerIndex, int playerCount)
    {
        foreach (Sentence sentence in playerControl.Voice.Sentences)
        {
            double startPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MinBeat);
            double endPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MaxBeat);
            if (playerCount <= 3)
            {
                // Show individual rectangles for each player
                float heightPercent = playerCount > 0 ? (100 / playerCount) : 100;
                float topPercent = playerIndex * heightPercent;
                MicProfile micProfile = playerControl.MicProfile;
                CreateRectangle(micProfile, startPosInMillis, endPosInMillis, durationOfSongInMillis, topPercent, heightPercent);
            }
            else
            {
                // Just show where the lyrics are, independent of the concrete player
                CreateRectangle(null, startPosInMillis, endPosInMillis, durationOfSongInMillis, 0, 100);
            }
        }
    }

    private void CreateRectangle(MicProfile micProfile, double startPosInMillis, double endPosInMillis, double durationOfSongInMillis, float topPercent, float heightPercent)
    {
        float startPosPercentage = (float)(100 * startPosInMillis / durationOfSongInMillis);
        float endPosPercentage = (float)(100 * endPosInMillis / durationOfSongInMillis);

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
