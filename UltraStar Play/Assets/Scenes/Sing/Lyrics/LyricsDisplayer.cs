using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LyricsDisplayer : MonoBehaviour
{
    public Text currentSentenceText;
    public Text nextSentenceText;

    public Sentence CurrentSentence { get; private set; }
    public List<Note> SortedNotes { get; private set; } = new List<Note>();

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

        IEnumerable<string> noteTexts = sentence.Notes.Select(it => it.Text);
        string joinedNoteTexts = string.Join("", noteTexts);
        return joinedNoteTexts;
    }
}
