using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingLyricsControl : INeedInjection, IInjectionFinishedListener
{
    private const float SpaceWidthInPx = 8;
    private const float MinFontSize = 4;
    private const float MaxFontSizeIterations = 20;

    public Sentence CurrentSentence { get; private set; }
    public List<Note> SortedNotes { get; private set; } = new();

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement rootVisualElement;

    [Inject(UxmlName = R.UxmlNames.currentSentenceContainer)]
    private VisualElement currentSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.plainLabelContainer)]
    private VisualElement plainLabelContainer;

    [Inject(UxmlName = R.UxmlNames.highlightLabelContainer)]
    private VisualElement highlightLabelContainer;

    [Inject(UxmlName = R.UxmlNames.nextSentenceContainer)]
    private VisualElement nextSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.positionBeforeLyricsIndicator)]
    private MaterialIcon positionBeforeLyricsIndicator;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private ThemeManager themeManager;

    private Sentence previousSentence;
    private readonly Dictionary<Note, Label> currentSentenceNoteToPlainLabelMap = new();
    private readonly Dictionary<Note, Label> currentSentenceNoteToHighlightLabelMap = new();
    private readonly Dictionary<Note, Label> nextSentenceNoteToPlainLabelMap = new();

    public Voice Voice => playerControl.Voice;

    private readonly List<int> fadeOutLyricsAnimationIds = new();

    public void OnInjectionFinished()
    {
        playerControl.EnterSentenceEventStream.Subscribe(enterSentenceEvent =>
        {
            Sentence nextSentence = playerControl.GetSentence(enterSentenceEvent.SentenceIndex + 1);
            SetCurrentSentence(enterSentenceEvent.Sentence);
            SetNextSentence(nextSentence);
        });

        ClearSentenceContainer(plainLabelContainer);
        ClearSentenceContainer(highlightLabelContainer);
        ClearSentenceContainer(nextSentenceContainer);

        SetCurrentSentence(playerControl.GetSentence(0));
        SetNextSentence(playerControl.GetSentence(1));

        // Before lyrics indicator
        GetCurrentNoteLyricsColor().IfNotDefault(color =>
        {
            positionBeforeLyricsIndicator.style.color = new StyleColor(color);
            positionBeforeLyricsIndicator.style.unityBackgroundImageTintColor = new StyleColor(color);
        });
        themeManager.GetCurrentTheme().ThemeJson.beforeLyricsIndicatorImage.IfNotNull(async path =>
        {
            if (path.IsNullOrEmpty())
            {
                return;
            }

            string absolutePath = ThemeMetaUtils.GetAbsoluteFilePath(themeManager.GetCurrentTheme(), path);
            Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(absolutePath);
            positionBeforeLyricsIndicator.style.backgroundImage = new StyleBackground(loadedSprite);
            positionBeforeLyricsIndicator.Icon = "";
        });
    }

    private Color32 GetPlayerControlColor()
    {
        if (playerControl != null
            && playerControl.MicProfile != null)
        {
            return playerControl.MicProfile.Color;
        }
        return Colors.clearBlack;
    }

    private Color32 GetCurrentNoteLyricsColor()
    {
        return themeManager.GetCurrentTheme().ThemeJson.currentNoteLyricsColor
            .OrIfDefault(GetPlayerControlColor());
    }

    private Color32 GetPreviousNoteLyricsColor()
    {
        return themeManager.GetCurrentTheme().ThemeJson.previousNoteLyricsColor
            .OrIfDefault(GetPlayerControlColor());
    }

    public void Update(double positionInMillis)
    {
        UpdateNoteHighlighting(positionInMillis);
        UpdatePositionBeforeLyricsIndicator(positionInMillis);
    }

    private void UpdatePositionBeforeLyricsIndicator(double positionInMillis)
    {
        if (CurrentSentence == null
            || CurrentSentence.Notes.IsNullOrEmpty()
            || SortedNotes.IsNullOrEmpty())
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        double previousSentenceEndInMillis = previousSentence != null
            ? SongMetaBpmUtils.BeatsToMillis(songMeta, previousSentence.ExtendedMaxBeat)
            : 0;
        double firstNoteStartBeatInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, CurrentSentence.MinBeat);

        if (Math.Abs(firstNoteStartBeatInMillis - previousSentenceEndInMillis) < 500)
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        double positionBeforeLyricsPercent = (positionInMillis - previousSentenceEndInMillis)
                                             / (firstNoteStartBeatInMillis - previousSentenceEndInMillis);

        // Find start position of label
        Note firstNote = SortedNotes[0];
        if (positionBeforeLyricsPercent is < 0 or > 1
            || !currentSentenceNoteToPlainLabelMap.TryGetValue(firstNote, out Label currentSentenceFirstLabel))
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        float labelMinX = currentSentenceFirstLabel.worldBound.xMin;
        float containerMinX = currentSentenceContainer.worldBound.xMin;
        float labelMinXRelativeToContainer = labelMinX - containerMinX;
        float positionBeforeLyricsPx = (float)(labelMinXRelativeToContainer * positionBeforeLyricsPercent);
        positionBeforeLyricsIndicator.ShowByDisplay();
        positionBeforeLyricsIndicator.style.left = positionBeforeLyricsPx;
    }

    private void UpdateNoteHighlighting(double positionInMillis)
    {
        Note currentNote = SortedNotes.FirstOrDefault(note =>
            SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat) <= positionInMillis
            && positionInMillis < SongMetaBpmUtils.BeatsToMillis(songMeta, note.EndBeat));
        HighlightNoteLyrics(positionInMillis, currentNote);
    }

    private void HighlightNoteLyrics(double positionInMillis, Note currentNote)
    {
        if (CurrentSentence == null)
        {
            return;
        }

        List<Note> sortedNotes = CurrentSentence.Notes.ToList();
        int currentNoteIndex = sortedNotes.IndexOf(currentNote);
        if (currentNoteIndex < 0)
        {
            return;
        }
        sortedNotes.Sort(Note.comparerByStartBeat);

        float maxHighlightLabelXMax = -1;

        for (int i = 0; i < sortedNotes.Count && i <= currentNoteIndex; i++)
        {
            Note note = sortedNotes[i];
            if (!currentSentenceNoteToHighlightLabelMap.TryGetValue(note, out Label label)
                || !currentSentenceNoteToPlainLabelMap.TryGetValue(note, out Label plainLabel))
            {
                continue;
            }

            // Use world bound of plain label because it has the correct world position.
            Rect plainLabelWorldBound = plainLabel.worldBound;

            if (i < currentNoteIndex)
            {
                label.AddToClassList(R.UssClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UssClasses.currentNoteLyrics);

                GetPreviousNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));

                maxHighlightLabelXMax = Mathf.Max(maxHighlightLabelXMax, plainLabelWorldBound.xMax);
            }
            else if (i == currentNoteIndex)
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.AddToClassList(R.UssClasses.currentNoteLyrics);

                GetCurrentNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));

                double noteStartInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat);
                double noteEndInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.EndBeat);
                double noteDurationInMillis = noteEndInMillis - noteStartInMillis;
                double noteDoneInMillis = positionInMillis - noteStartInMillis;
                if (noteDurationInMillis > 0
                    && noteDoneInMillis > 0)
                {
                    double noteDoneFactor = noteDoneInMillis / noteDurationInMillis;
                    noteDoneFactor = NumberUtils.Limit(noteDoneFactor, 0, 1);

                    double labelDoneX = plainLabelWorldBound.xMin + (noteDoneFactor * plainLabelWorldBound.width);
                    maxHighlightLabelXMax = Mathf.Ceil(Mathf.Max(maxHighlightLabelXMax, (float)labelDoneX));
                }
            }
            else
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UssClasses.currentNoteLyrics);

                GetPreviousNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));
            }
        }

        // Update width of label container for wipe effect
        if (settings.WipeLyrics)
        {
            if (maxHighlightLabelXMax > 0)
            {
                float highlightLabelContainerWidth = maxHighlightLabelXMax - highlightLabelContainer.worldBound.x;
                highlightLabelContainer.style.width = Mathf.Ceil(highlightLabelContainerWidth);
            }
            else
            {
                highlightLabelContainer.style.width = 0;
            }
        }
        else
        {
            highlightLabelContainer.style.width = new StyleLength(StyleKeyword.Auto);
        }
    }

    private void SetCurrentSentence(Sentence sentence)
    {
        previousSentence = CurrentSentence;
        CurrentSentence = sentence;
        if (CurrentSentence != null)
        {
            SortedNotes = new List<Note>(sentence.Notes);
            SortedNotes.Sort(Note.comparerByStartBeat);
            FillSentenceContainer(plainLabelContainer, CurrentSentence, false, currentSentenceNoteToPlainLabelMap);
            FillSentenceContainer(highlightLabelContainer, CurrentSentence, false, currentSentenceNoteToHighlightLabelMap);
            UpdateFontSize(plainLabelContainer);
            UpdateFontSize(highlightLabelContainer);
        }
        else
        {
            // After last sentence => fade out the current lyrics
            SortedNotes = new List<Note>();
            LeanTween.value(gameObject, currentSentenceContainer.resolvedStyle.opacity, 0, 1f)
                .setOnUpdate(interpolatedValue =>
                {
                    currentSentenceContainer.style.opacity = interpolatedValue;
                });
        }
    }

    private void UpdateFontSize(VisualElement labelContainer)
    {
        List<Label> labels = labelContainer.Query<Label>().ToList();
        if (labels.IsNullOrEmpty())
        {
            return;
        }

        float availableWidth = currentSentenceContainer.resolvedStyle.width;

        // When all labels are ready (i.e. they have well defined geometry) then update their font size.
        List<Label> labelsWithoutGeometry = labels.Where(label => !VisualElementUtils.HasGeometry(label)).ToList();
        if (labelsWithoutGeometry.IsNullOrEmpty())
        {
            DoUpdateFontSize(labels, availableWidth);
        }
        else
        {
            foreach (Label label in labels)
            {
                label.RegisterCallbackOneShot<GeometryChangedEvent>(_ =>
                {
                    labelsWithoutGeometry.Remove(label);
                    if (labelsWithoutGeometry.IsNullOrEmpty())
                    {
                        DoUpdateFontSize(labels, availableWidth);
                    }
                });
            }
        }
    }

    /**
     * Reduces the font size until all labels fit in the container
     */
    private void DoUpdateFontSize(List<Label> labels, float availableWidth)
    {
        if (labels.IsNullOrEmpty())
        {
            Debug.Log("No labels");
            return;
        }

        float fontSize = labels.FirstOrDefault().resolvedStyle.fontSize;
        // TODO: binary search for better performance.
        for (int iteration = 0; iteration < MaxFontSizeIterations; iteration++)
        {
            float totalLabelWidth = GetTotalLabelWidth(labels);
            if (totalLabelWidth > availableWidth)
            {
                if (fontSize <= MinFontSize)
                {
                    // Required font size is too small
                    Debug.Log("Required font size is too small, aborting optimal font size search");
                    break;
                }
                fontSize -= 1;
                labels.ForEach(label => label.style.fontSize = fontSize);
            }
            else
            {
                // All labels fit in the container
                break;
            }
        }
    }

    private float GetTotalLabelWidth(List<Label> labels)
    {
        return labels.Select(label =>
        {
            IResolvedStyle resolvedStyle = label.resolvedStyle;
            if (label.ClassListContains(R.UssClasses.singingLyricsSpace))
            {
                return resolvedStyle.width;
            }

            Vector2 preferredTextSize = label.GetPreferredTextSize();
            return resolvedStyle.marginLeft + preferredTextSize.x + resolvedStyle.marginRight;
        }).Sum();
    }

    private void ClearSentenceContainer(VisualElement visualElement)
    {
        visualElement.Clear();
    }

    private void FillSentenceContainer(VisualElement visualElement, Sentence sentence, bool isNextSentence, Dictionary<Note, Label> noteToLabel)
    {
        ClearSentenceContainer(visualElement);

        noteToLabel.Clear();

        if (sentence == null
            || sentence.Notes.IsNullOrEmpty())
        {
            visualElement.Add(new Label(" "));
            return;
        }

        List<string> labelUssClasses = new()
        {
            R.UssClasses.singingLyrics,
        };
        if (visualElement == plainLabelContainer
            || visualElement == highlightLabelContainer)
        {
            labelUssClasses.Add(R.UssClasses.currentLyrics);
        }
        else if (visualElement == nextSentenceContainer)
        {
            labelUssClasses.Add(R.UssClasses.nextLyrics);
        }

        List<Note> sortedNotes = sentence.Notes.ToList();
        sortedNotes.Sort(Note.comparerByStartBeat);
        sortedNotes.ForEach(note =>
        {
            // Show underscore as space.
            // Underscore is used in song editor to show notes with missing lyrics after speech recognition.
            string displayText = note.Text.Replace("_", " ");
            string richText = IsItalicDisplayText(note.Type)
                ? $"<i>{displayText.Trim()}</i>"
                : displayText.Trim();

            Label label = new(richText);
            Label spaceLabel = null;

            labelUssClasses.ForEach(ussClass => label.AddToClassList(ussClass));

            noteToLabel.Add(note, label);

            label.enableRichText = true;

            if (displayText.StartsWith(" ")
                || displayText.EndsWith(" "))
            {
                spaceLabel = new("");
                labelUssClasses.ForEach(ussClass => spaceLabel.AddToClassList(ussClass));
                spaceLabel.AddToClassList(R.UssClasses.singingLyricsSpace);
                visualElement.Add(spaceLabel);
            }

            ThemeMeta currentThemeMeta = themeManager.GetCurrentTheme();
            if (isNextSentence)
            {
                currentThemeMeta.ThemeJson.nextLyricsColor
                    .OrIfDefault(currentThemeMeta.ThemeJson.lyricsColor)
                    .IfNotDefault(color => label.style.color = new StyleColor(color));
            }
            else
            {
                currentThemeMeta.ThemeJson.lyricsColor
                    .IfNotDefault(color => label.style.color = new StyleColor(color));
            }
            currentThemeMeta.ThemeJson.lyricsOutlineColor
                .IfNotDefault(color => label.style.unityTextOutlineColor = new StyleColor(color));
            if (!currentThemeMeta.ThemeJson.lyricsShadow)
            {
                label.style.textShadow = new StyleTextShadow();
            }

            if (spaceLabel != null
                && displayText.StartsWith(" "))
            {
                visualElement.Add(spaceLabel);
            }
            visualElement.Add(label);
            if (spaceLabel != null
                && displayText.EndsWith(" "))
            {
                visualElement.Add(spaceLabel);
            }
        });
    }

    private void SetNextSentence(Sentence sentence)
    {
        FillSentenceContainer(nextSentenceContainer, sentence, true, nextSentenceNoteToPlainLabelMap);
        UpdateFontSize(nextSentenceContainer);
    }

    private static bool IsItalicDisplayText(ENoteType type)
    {
        switch (type)
        {
            case ENoteType.Freestyle:
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                return true;
            default:
                return false;
        }
    }

    public void FadeOut(float animTimeInSeconds)
    {
        LeanTweenUtils.CancelAndClear(fadeOutLyricsAnimationIds);
        fadeOutLyricsAnimationIds.Add(AnimationUtils.FadeOutVisualElement(gameObject, currentSentenceContainer, animTimeInSeconds));
        fadeOutLyricsAnimationIds.Add(AnimationUtils.FadeOutVisualElement(gameObject, nextSentenceContainer, animTimeInSeconds));
    }

    public void FadeIn(float animTimeInSeconds)
    {
        LeanTweenUtils.CancelAndClear(fadeOutLyricsAnimationIds);
        fadeOutLyricsAnimationIds.Add(AnimationUtils.FadeInVisualElement(gameObject, currentSentenceContainer, animTimeInSeconds));
        fadeOutLyricsAnimationIds.Add(AnimationUtils.FadeInVisualElement(gameObject, nextSentenceContainer, animTimeInSeconds));
    }
}
