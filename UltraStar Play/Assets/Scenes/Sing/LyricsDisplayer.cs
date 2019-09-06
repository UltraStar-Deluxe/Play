using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LyricsDisplayer : MonoBehaviour
{
    public Text currentSentenceText;
    public Text nextSentenceText;

    public Sentence CurrentSentence { get; private set; }

    public void SetCurrentSentence(Sentence sentence)
    {
        CurrentSentence = sentence;
        currentSentenceText.text = CreateStringFromSentence(sentence);
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
