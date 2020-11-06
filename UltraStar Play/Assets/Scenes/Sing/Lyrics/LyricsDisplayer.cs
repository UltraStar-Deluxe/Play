using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LyricsDisplayer : MonoBehaviour, INeedInjection
{
    public Text currentSentenceText;
    public Text nextSentenceText;

    public bool isTop;

    public Sentence CurrentSentence { get; private set; }
    public List<Note> SortedNotes { get; private set; } = new List<Note>();

    [Inject]
    private Settings settings;

    void Start()
    {
        // The ScrollingNoteStreamDisplayer shows the lyrics below the notes. Thus, there is no need for this LyricsDisplayer.
        if (settings.GraphicSettings.noteDisplayMode == ENoteDisplayMode.ScrollingNoteStream)
        {
            gameObject.SetActive(false);
        }
    }

    public void Init(PlayerController playerController)
    {
        playerController.EnterSentenceEventStream.Subscribe(enterSentenceEvent =>
        {
            Sentence nextSentence = playerController.GetSentence(enterSentenceEvent.SentenceIndex + 1);
            SetCurrentSentence(enterSentenceEvent.Sentence);
            SetNextSentence(nextSentence);
        });

        SetCurrentSentence(playerController.GetSentence(0));
        SetNextSentence(playerController.GetSentence(1));
    }

    public void SetCurrentSentence(Sentence sentence)
    {
        CurrentSentence = sentence;
        if (CurrentSentence != null)
        {
            SortedNotes = new List<Note>(sentence.Notes);
            SortedNotes.Sort(Note.comparerByStartBeat);
            currentSentenceText.text = CreateStringFromSentence(sentence);
        }
        else
        {
            SortedNotes = new List<Note>();
            currentSentenceText.text = "";
        }
    }

    public void SetNextSentence(Sentence sentence)
    {
        nextSentenceText.text = CreateStringFromSentence(sentence);
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
