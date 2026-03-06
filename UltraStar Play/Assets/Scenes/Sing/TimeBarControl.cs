using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.innerTimeBarSentenceEntryContainer)]
    private VisualElement innerTimeBarSentenceEntryContainer;

    [Inject(UxmlName = R.UxmlNames.timeBarPositionIndicator)]
    private VisualElement timeBarPositionIndicator;

    [Inject(UxmlName = R.UxmlNames.timeValueLabel)]
    private Label timeValueLabel;

    [Inject(UxmlName = R.UxmlNames.timeBarsContainer)]
    private VisualElement timeBarsContainer;

    [Inject(UxmlName = R.UxmlNames.timeBarLyricsPreviewLabel)]
    private Label timeBarLyricsPreviewLabel;

    [Inject(UxmlName = R.UxmlNames.timeBarLyricsPreviewShadow)]
    private VisualElement timeBarLyricsPreviewShadow;

    [Inject] private SingSceneMedleyControl medleyControl;

    [Inject] private SongMeta songMeta;

    [Inject] private SongAudioPlayer songAudioPlayer;

    [Inject] private CursorManager cursorManager;

    [Inject] private SingSceneControl singSceneControl;

    private double LateStartInSongInMillis => songMeta?.StartInMillis ?? 0;
    private double EarlyEndInSongInMillis => songMeta?.EndInMillis ?? 0;

    public void OnInjectionFinished()
    {
        timeBarLyricsPreviewLabel.HideByDisplay();

        // Change positon in audio by clicking time bar
        timeBarsContainer.RegisterCallback<PointerDownEvent>(OnTimeBarClicked);
        timeBarsContainer.RegisterCallback<PointerEnterEvent>(OnTimeBarEnter);
        timeBarsContainer.RegisterCallback<PointerLeaveEvent>(OnTimeBarLeave);
        timeBarsContainer.RegisterCallback<PointerMoveEvent>(OnTimeBarPointerMove);
    }

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

        double remainingTimeInSeconds =
            (durationInMillisConsideringStartAndEndTag - positionConsideringStartTag) / 1000;
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

        float positionInPercent =
            (float)(100 * positionConsideringStartTag / durationInMillisConsideringStartAndEndTag);
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

    private void OnTimeBarClicked(IPointerEvent evt)
    {
        // Do not trigger other events, e.g., do not trigger play / pause
        (evt as EventBase)?.StopImmediatePropagation();

        // Calculate click position as percentage within the time bar
        float width = timeBarsContainer.contentRect.width;
        if (width <= 0f)
        {
            return;
        }

        float x = evt.localPosition.x;
        float ratio = Mathf.Clamp01(x / width);

        double startTagInMillis = songMeta.StartInMillis;
        double endTagInMillis = songMeta.EndInMillis;
        double durationInMillisConsideringStartAndEndTag =
            songAudioPlayer.DurationInMillis - startTagInMillis - endTagInMillis;
        if (durationInMillisConsideringStartAndEndTag <= 0)
        {
            return;
        }

        double targetPositionConsideringStartAndEnd = ratio * durationInMillisConsideringStartAndEndTag;
        double targetPositionInMillis = startTagInMillis + targetPositionConsideringStartAndEnd;
        singSceneControl.JumpToAudioPositionByUserAction(targetPositionInMillis);
    }

    private void OnTimeBarEnter(PointerEnterEvent evt)
    {
        cursorManager.SetCursorHand();
        UpdateLyricsPreviewForPointer(evt.localPosition.x);
    }

    private void OnTimeBarLeave(PointerLeaveEvent evt)
    {
        cursorManager.SetDefaultCursor();
        timeBarLyricsPreviewLabel.HideByDisplay();
    }

    private void OnTimeBarPointerMove(PointerMoveEvent evt)
    {
        UpdateLyricsPreviewForPointer(evt.localPosition.x);
    }

    private void UpdateLyricsPreviewForPointer(float localMouseX)
    {
        float width = timeBarsContainer.contentRect.width;
        if (width <= 0f
            || songMeta == null
            || songAudioPlayer == null)
        {
            timeBarLyricsPreviewLabel.HideByDisplay();
            return;
        }

        timeBarLyricsPreviewShadow.style.left = new StyleLength(new Length(localMouseX, LengthUnit.Pixel));

        float clampedX = Mathf.Clamp(localMouseX, 0, width);
        float ratio = width > 0 ? Mathf.Clamp01(clampedX / width) : 0f;

        double startTagInMillis = songMeta.StartInMillis;
        double endTagInMillis = songMeta.EndInMillis;
        double durationInMillisConsideringStartAndEndTag =
            songAudioPlayer.DurationInMillis - startTagInMillis - endTagInMillis;
        if (durationInMillisConsideringStartAndEndTag <= 0)
        {
            timeBarLyricsPreviewLabel.HideByDisplay();
            return;
        }

        double positionConsideringStartAndEnd = ratio * durationInMillisConsideringStartAndEndTag;
        double positionInMillis = startTagInMillis + positionConsideringStartAndEnd;

        string preview = GetUpcomingLyricsAt(positionInMillis);
        if (preview.IsNullOrEmpty())
        {
            timeBarLyricsPreviewLabel.HideByDisplay();
        }
        else
        {
            timeBarLyricsPreviewLabel.ShowByDisplay();
            timeBarLyricsPreviewLabel.text = preview;
        }
    }

    private string GetUpcomingLyricsAt(double positionInMillis)
    {
        if (songMeta == null)
        {
            return "";
        }

        double beat = SongMetaBpmUtils.MillisToBeats(songMeta, positionInMillis);

        // Find the earliest sentence (across all players/voices) that is at or after this beat,
        // or the current sentence if the beat is inside one.
        Sentence bestSentence = null;
        int bestStartBeat = int.MaxValue;

        if (singSceneControl?.PlayerControls == null)
        {
            return "";
        }

        foreach (PlayerControl pc in singSceneControl.PlayerControls)
        {
            if (pc?.Voice == null)
            {
                continue;
            }

            foreach (Sentence s in pc.Voice.Sentences)
            {
                if (!medleyControl.IsSentenceInMedleyRange(s))
                {
                    continue;
                }

                // If pointer is inside the sentence, prefer this sentence immediately.
                bool inside = SongMetaUtils.IsBeatInSentence(s, (int)Math.Round(beat), true, false);
                if (inside)
                {
                    bestSentence = s;
                    bestStartBeat = s.MinBeat;
                    break;
                }

                // Otherwise consider next sentence starting after the beat
                if (s.MinBeat >= beat && s.MinBeat < bestStartBeat)
                {
                    bestSentence = s;
                    bestStartBeat = s.MinBeat;
                }
            }
        }

        if (bestSentence == null)
        {
            return "";
        }

        // Build lyrics for the whole sentence (upcoming or surrounding)
        List<Note> notes = SongMetaUtils.GetSortedNotes(bestSentence);
        if (notes.IsNullOrEmpty())
        {
            return "";
        }

        string result = SongMetaUtils.GetLyrics(bestSentence);
        // Replace '_' like in regular lyrics display
        result = result.Replace("_", " ");
        // Trim tildes at the edges for nicer preview
        result = result.Trim('~', ' ');
        return result;
    }

    private void CreateRectangles(SongMeta songMeta, PlayerControl playerControl, double durationInMillis,
        int playerIndex, int playerCount)
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
                CreateRectangle(micProfile, startPosInMillis, endPosInMillis, durationInMillis, topPercent,
                    heightPercent);
            }
            else
            {
                // Just show where the lyrics are, independent of the concrete player
                CreateRectangle(null, startPosInMillis, endPosInMillis, durationInMillis, 0, 100);
            }
        }
    }

    private void CreateRectangle(MicProfile micProfile, double startPosInMillis, double endPosInMillis,
        double durationInMillis, float topPercent, float heightPercent)
    {
        double durationInMillisConsideringEndTag = EarlyEndInSongInMillis > 0
            ? Math.Min(durationInMillis, EarlyEndInSongInMillis)
            : durationInMillis;
        double durationInMillisConsideringStartAndEndTag = durationInMillisConsideringEndTag - LateStartInSongInMillis;
        double startPosInMillisConsideringStartTag = startPosInMillis - LateStartInSongInMillis;
        double endPosInMillisConsideringStartTag = endPosInMillis - LateStartInSongInMillis;

        float startPosPercentage =
            (float)(100 * startPosInMillisConsideringStartTag / durationInMillisConsideringStartAndEndTag);
        float endPosPercentage =
            (float)(100 * endPosInMillisConsideringStartTag / durationInMillisConsideringStartAndEndTag);

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
