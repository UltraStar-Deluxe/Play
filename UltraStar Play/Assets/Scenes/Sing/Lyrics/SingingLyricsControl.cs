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

    [Inject(UxmlName = R.UxmlNames.currentSentenceLabel)]
    private Label currentSentenceLabel;

    [Inject(UxmlName = R.UxmlNames.nextSentenceLabel)]
    private Label nextSentenceLabel;

    [Inject]
    private Settings settings;

    [Inject]
    private PlayerControl playerControl;

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

    public void SetCurrentSentence(Sentence sentence)
    {
        CurrentSentence = sentence;
        if (CurrentSentence != null)
        {
            SortedNotes = new List<Note>(sentence.Notes);
            SortedNotes.Sort(Note.comparerByStartBeat);
            currentSentenceLabel.text = CreateStringFromSentence(sentence);
        }
        else
        {
            SortedNotes = new List<Note>();
            currentSentenceLabel.text = "";
        }
    }

    public void SetNextSentence(Sentence sentence)
    {
        nextSentenceLabel.text = CreateStringFromSentence(sentence);
    }

    private string CreateStringFromSentence(Sentence sentence)
    {
        if (sentence == null)
        {
            return "";
        }

        string result = "";
        foreach (Note note in sentence.Notes)
        {
            if (IsItalicDisplayText(note.Type))
            {
                result += $"<i>{note.Text}</i>";
            }
            else
            {
                result += note.Text;
            }
        }
        return result;
    }

    private bool IsItalicDisplayText(ENoteType type)
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
