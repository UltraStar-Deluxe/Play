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

    public Sentence CurrentSentence { get; private set; }
    public List<Note> SortedNotes { get; private set; } = new();

    [Inject(UxmlName = R.UxmlNames.currentSentenceContainer)]
    private VisualElement currentSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.nextSentenceContainer)]
    private VisualElement nextSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.positionBeforeLyricsIndicator)]
    private VisualElement positionBeforeLyricsIndicator;

    [Inject]
    private Settings settings;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private ThemeManager themeManager;
    
    private Sentence previousSentence;
    private readonly Dictionary<Note, Label> currentSentenceNoteToLabelMap = new();

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
        
        themeManager.GetCurrentTheme().ThemeJson.currentNoteLyricsColor.IfNotDefault(color =>
            positionBeforeLyricsIndicator.style.color = new StyleColor(color));
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
                
                themeManager.GetCurrentTheme().ThemeJson.previousNoteLyricsColor.IfNotDefault(color =>
                    label.style.color = new StyleColor(color));
            }
            else if (i == currentNoteIndex)
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.AddToClassList(R.UssClasses.currentNoteLyrics);
                
                themeManager.GetCurrentTheme().ThemeJson.currentNoteLyricsColor.IfNotDefault(color =>
                    label.style.color = new StyleColor(color));
            }
            else
            {
                label.RemoveFromClassList(R.UssClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UssClasses.currentNoteLyrics);
                
                themeManager.GetCurrentTheme().ThemeJson.lyricsColor.IfNotDefault(color =>
                    label.style.color = new StyleColor(color));
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
        }
        else
        {
            SortedNotes = new List<Note>();
        }
        FillContainerWithSentenceText(currentSentenceContainer, CurrentSentence);
    }

    private void FillContainerWithSentenceText(VisualElement visualElement, Sentence sentence)
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
            string richText = IsItalicDisplayText(note.Type)
                ? $"<i>{note.Text.Trim()}</i>"
                : note.Text.Trim();

            Label label = new(richText);
            label.enableRichText = true;

            if (note.Text.StartsWith(" "))
            {
                label.style.marginLeft = SpaceWidthInPx;
            }
            if (note.Text.EndsWith(" "))
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

            themeManager.GetCurrentTheme().ThemeJson.lyricsColor.IfNotDefault(color =>
                label.style.color = new StyleColor(color));
            themeManager.GetCurrentTheme().ThemeJson.lyricsOutlineColor.IfNotDefault(color =>
                label.style.unityTextOutlineColor = new StyleColor(color));
            
            visualElement.Add(label);
        });
    }

    private void SetNextSentence(Sentence sentence)
    {
        FillContainerWithSentenceText(nextSentenceContainer, sentence);
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
}
