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
    public Sentence CurrentSentence { get; private set; }
    public List<Note> SortedNotes { get; private set; } = new List<Note>();

    [Inject(UxmlName = R.UxmlNames.currentSentenceContainer)]
    private VisualElement currentSentenceContainer;

    [Inject(UxmlName = R.UxmlNames.nextSentenceContainer)]
    private VisualElement nextSentenceContainer;

    [Inject]
    private Settings settings;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private SongMeta songMeta;

    private Dictionary<Note, Label> currentSentenceNoteToLabelMap = new Dictionary<Note, Label>();

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
    }

    public void UpdateNoteHighlighting(double positionInSongInMillis)
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

        int currentNoteIndex = CurrentSentence.Notes.IndexOf(currentNote);
        if (currentNoteIndex < 0)
        {
            return;
        }

        for (int i = 0; i < CurrentSentence.Notes.Count && i <= currentNoteIndex; i++)
        {
            Note note = SortedNotes[i];
            if (!currentSentenceNoteToLabelMap.TryGetValue(note, out Label label))
            {
                continue;
            }

            if (i < currentNoteIndex)
            {
                label.AddToClassList(R.UxmlClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UxmlClasses.currentNoteLyrics);
            }
            else if (i == currentNoteIndex)
            {
                label.RemoveFromClassList(R.UxmlClasses.previousNoteLyrics);
                label.AddToClassList(R.UxmlClasses.currentNoteLyrics);
            }
            else
            {
                label.RemoveFromClassList(R.UxmlClasses.previousNoteLyrics);
                label.RemoveFromClassList(R.UxmlClasses.currentNoteLyrics);
            }
        }
    }

    private void SetCurrentSentence(Sentence sentence)
    {
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
        visualElement.Clear();
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

        sentence.Notes.ForEach(note =>
        {
            string richText = IsItalicDisplayText(note.Type)
                ? $"<i>{note.Text}</i>"
                : note.Text;
            Label label = new Label(richText);
            label.enableRichText = true;
            label.AddToClassList(R.UxmlClasses.singingLyrics);
            if (visualElement == currentSentenceContainer)
            {
                label.AddToClassList(R.UxmlClasses.currentLyrics);
                currentSentenceNoteToLabelMap.Add(note, label);
            }
            else if (visualElement == nextSentenceContainer)
            {
                label.AddToClassList(R.UxmlClasses.nextLyrics);
            }

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
        }
        return false;
    }
}
