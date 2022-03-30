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

    [Inject(UxmlName = R.UxmlNames.timeLabel)]
    private Label timeLabel;

    [Inject(UxmlName = R.UxmlNames.timeValueLabel)]
    private Label timeValueLabel;

    public void UpdateTimeValueLabel(float positionInSongInMillis, float durationOfSongInMillis)
    {
        float remainingTimeInSeconds = (durationOfSongInMillis - positionInSongInMillis) / 1000f;
        int mins = (int)Math.Floor(remainingTimeInSeconds / 60);
        string minsPadding = (mins < 10) ? "0" : "";
        int secs = (int)Math.Floor(remainingTimeInSeconds % 60);
        string secsPadding = (secs < 10) ? "0" : "";
        timeValueLabel.text = $"{minsPadding}{mins}:{secsPadding}{secs}";
    }

    public void UpdatePositionIndicator(float positionInSongInMillis, float durationOfSongInMillis)
    {
        float positionInPercent = 100f * positionInSongInMillis / durationOfSongInMillis;
        timeBarPositionIndicator.style.left = new StyleLength(new Length(positionInPercent, LengthUnit.Percent));
    }

    public void UpdateTimeBarRectangles(SongMeta songMeta, List<PlayerControl> playerControls, float durationOfSongInMillis)
    {
        innerTimeBarSentenceEntryContainer.Clear();
        foreach (PlayerControl playerController in playerControls)
        {
            CreateRectangles(songMeta, playerController, durationOfSongInMillis);
        }
    }

    private void CreateRectangles(SongMeta songMeta, PlayerControl playerControl, float durationOfSongInMillis)
    {
        foreach (Sentence sentence in playerControl.Voice.Sentences)
        {
            float startPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MinBeat);
            float endPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MaxBeat);
            MicProfile micProfile = playerControl.MicProfile;
            CreateRectangle(micProfile, startPosInMillis, endPosInMillis, durationOfSongInMillis);
        }
    }

    private void CreateRectangle(MicProfile micProfile, float startPosInMillis, float endPosInMillis, float durationOfSongInMillis)
    {
        float startPosPercentage = 100f * startPosInMillis / durationOfSongInMillis;
        float endPosPercentage = 100f * endPosInMillis / durationOfSongInMillis;

        VisualElement rectangle = new();
        rectangle.style.position = new StyleEnum<Position>(Position.Absolute);
        rectangle.style.left = new StyleLength(new Length(startPosPercentage, LengthUnit.Percent));
        rectangle.style.width = new StyleLength(new Length(endPosPercentage - startPosPercentage, LengthUnit.Percent));
        rectangle.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

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
