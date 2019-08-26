using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LyricsDisplayer : MonoBehaviour
{
    public Text CurrentSentenceText;
    public Text NextSentenceText;

    public void SetCurrentSentence(Sentence sentence)
    {
        CurrentSentenceText.text = CreateStringFromSentence(sentence);
    }

    public void SetNextSentence(Sentence sentence)
    {
        NextSentenceText.text = CreateStringFromSentence(sentence);
    }

    private string CreateStringFromSentence(Sentence sentence)
    {
        if(sentence == null) {
            return "";
        }

        var noteTexts = sentence.Notes.Select(it => it.Text);
        var joinedNoteTexts = string.Join("", noteTexts);
        return joinedNoteTexts;
    }
}
