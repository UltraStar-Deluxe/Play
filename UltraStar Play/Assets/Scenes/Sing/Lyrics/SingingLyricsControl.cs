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

    [Inject(UxmlName = R.UxmlNames.nextSentenceContainer)]
    private VisualElement nextSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.positionBeforeLyricsIndicator)]
    private VisualElement positionBeforeLyricsIndicator;

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
    private readonly Dictionary<Note, Label> currentSentenceNoteToLabelMap = new();

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

        SetCurrentSentence(playerControl.GetSentence(0));
        SetNextSentence(playerControl.GetSentence(1));

        GetCurrentNoteLyricsColor().IfNotDefault(color => positionBeforeLyricsIndicator.style.color = new StyleColor(color));
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
    
    public void Update(double positionInSongInMillis)
    {
        UpdateNoteHighlighting(positionInSongInMillis);
        UpdatePositionBeforeLyricsIndicator(positionInSongInMillis);
    }

    private void UpdatePositionBeforeLyricsIndicator(double positionInSongInMillis)
    {
        if (CurrentSentence == null
            || CurrentSentence.Notes.IsNullOrEmpty()
            || SortedNotes.IsNullOrEmpty())
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        double previousSentenceEndInMillis = previousSentence != null
            ? BpmUtils.BeatToMillisecondsInSong(songMeta, previousSentence.ExtendedMaxBeat)
            : 0;
        double firstNoteStartBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, CurrentSentence.MinBeat);

        if (Math.Abs(firstNoteStartBeatInMillis - previousSentenceEndInMillis) < 500)
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        double positionBeforeLyricsPercent = (positionInSongInMillis - previousSentenceEndInMillis)
                                             / (firstNoteStartBeatInMillis - previousSentenceEndInMillis);

        // Find start position of label
        Note firstNote = SortedNotes[0];
        if (positionBeforeLyricsPercent is < 0 or > 1
            || !currentSentenceNoteToLabelMap.TryGetValue(firstNote, out Label firstLabel))
        {
            positionBeforeLyricsIndicator.HideByDisplay();
            return;
        }

        float labelMinX = firstLabel.worldBound.xMin;
        float containerMinX = currentSentenceContainer.worldBound.xMin;
        float labelMinXRelativeToContainer = labelMinX - containerMinX;
        float positionBeforeLyricsPx = (float)(labelMinXRelativeToContainer * positionBeforeLyricsPercent);
        positionBeforeLyricsIndicator.ShowByDisplay();
        positionBeforeLyricsIndicator.style.left = positionBeforeLyricsPx;
    }

    private void UpdateNoteHighlighting(double positionInSongInMillis)
    {
        Note currentNote = SortedNotes
            .FirstOrDefault(note => BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat) <= positionInSongInMillis
                                    && positionInSongInMillis <= BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat));
        HighlightNoteLyrics(currentNote);
    }

    private void HighlightNoteLyrics(Note currentNote)
    {
        if (CurrentSentence == null)
        {
            return;
        }

        List<Note> sortedNotes = CurrentSentence.Notes.ToList();
        sortedNotes.Sort(Note.comparerByStartBeat);
        int currentNoteIndex = sortedNotes.IndexOf(currentNote);
        if (currentNoteIndex < 0)
        {
            return;
        }

        for (int i = 0; i < sortedNotes.Count && i <= currentNoteIndex; i++)
        {
            Note note = sortedNotes[i];
            if (!currentSentenceNoteToLabelMap.TryGetValue(note, out Label label))
            {
                continue;
            }

            if (i < currentNoteIndex)
            {
                label.AddToClassList(R.UssClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UssClasses.currentNoteLyrics);
                
                GetPreviousNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));
            }
            else if (i == currentNoteIndex)
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.AddToClassList(R.UssClasses.currentNoteLyrics);
                
                GetCurrentNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));
            }
            else
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UssClasses.currentNoteLyrics);
                
                GetPreviousNoteLyricsColor().IfNotDefault(color => label.style.color = new StyleColor(color));
            }
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
            FillContainerWithSentenceText(currentSentenceContainer, CurrentSentence, false);
            UpdateFontSize(currentSentenceContainer);
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

    private void UpdateFontSize(VisualElement visualElement)
    {
        List<Label> labels = visualElement.Query<Label>().ToList();
        if (labels.IsNullOrEmpty())
        {
            return;
        }

        // When all labels are ready (i.e. they have well defined geometry) then update their font size.
        List<Label> labelsWithoutGeometry = labels.Where(label => !VisualElementUtils.HasGeometry(label)).ToList();
        if (labelsWithoutGeometry.IsNullOrEmpty())
        {
            DoUpdateFontSize(visualElement, labels);
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
                        DoUpdateFontSize(visualElement, labels);
                    }
                });
            }
        }
    }
    
    /**
     * Reduces the font size until all labels fit in the container
     */
    private void DoUpdateFontSize(VisualElement visualElement, List<Label> labels)
    {
        if (labels.IsNullOrEmpty())
        {
            Debug.Log("No labels");
            return;
        }

        float fontSize = labels.FirstOrDefault().resolvedStyle.fontSize;
        float containerWidth = visualElement.contentRect.width;
        // TODO: binary search for better performance.
        for (int iteration = 0; iteration < MaxFontSizeIterations; iteration++)
        {
            float totalLabelWidth = GetTotalLabelWidth(labels);
            if (totalLabelWidth > containerWidth)
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
            Vector2 preferredTextSize = label.GetPreferredTextSize();
            IResolvedStyle resolvedStyle = label.resolvedStyle;
            return resolvedStyle.marginLeft + preferredTextSize.x + resolvedStyle.marginRight;
        }).Sum();
    }
    
    private void FillContainerWithSentenceText(VisualElement visualElement, Sentence sentence, bool isNextSentence)
    {
        visualElement.Query<Label>()
            .ToList()
            .ForEach(label =>
            {
                if (label != positionBeforeLyricsIndicator)
                {
                    label.RemoveFromHierarchy();
                }
            });
        if (visualElement == currentSentenceContainer)
        {
            currentSentenceNoteToLabelMap.Clear();
        }

        if (sentence == null
            || sentence.Notes.IsNullOrEmpty())
        {
            visualElement.Add(new Label(" "));
            return;
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
            label.enableRichText = true;

            if (displayText.StartsWith(" "))
            {
                label.style.marginLeft = SpaceWidthInPx;
            }
            if (displayText.EndsWith(" "))
            {
                label.style.marginRight = SpaceWidthInPx;
            }

            label.AddToClassList(R.UssClasses.singingLyrics);
            if (visualElement == currentSentenceContainer)
            {
                label.AddToClassList(R.UssClasses.currentLyrics);
                currentSentenceNoteToLabelMap.Add(note, label);
            }
            else if (visualElement == nextSentenceContainer)
            {
                label.AddToClassList(R.UssClasses.nextLyrics);
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
            
            visualElement.Add(label);
        });
    }

    private void SetNextSentence(Sentence sentence)
    {
        FillContainerWithSentenceText(nextSentenceContainer, sentence, true);
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
