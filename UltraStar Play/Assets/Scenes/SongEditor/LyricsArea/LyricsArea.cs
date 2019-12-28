using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Text;

#pragma warning disable CS0649

public class LyricsArea : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private List<Sentence> sortedSentences;

    private int lastCaretPosition;

    private string lyrics;

    void Start()
    {
        lyrics = GetLyrics(voices);
        inputField.text = lyrics;
        inputField.onEndEdit.AsObservable().Subscribe(OnEndEdit);
    }

    void Update()
    {
        if (lastCaretPosition != inputField.caretPosition)
        {
            lastCaretPosition = inputField.caretPosition;

            SyncPositionInSongWithSelectedText();
        }
    }

    private void OnEndEdit(string newText)
    {
        // TODO: Change the lyrics if only the lyrics for a single note changed
        // Ignore new lyrics for now.
        inputField.text = lyrics;
    }

    private void SyncPositionInSongWithSelectedText()
    {
        Note note = GetNoteForPositionInLyrics(inputField.caretPosition);
        if (note != null)
        {
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
    }

    private string GetLyrics(List<Voice> voices)
    {
        sortedSentences = voices.SelectMany(voice => voice.Sentences).ToList();
        sortedSentences.Sort((s1, s2) => s1.MinBeat.CompareTo(s2.MinBeat));
        StringBuilder stringBuilder = new StringBuilder();
        foreach (Sentence sentence in sortedSentences)
        {
            string sentenceText = sentence.Notes.Select(note => note.Text).ToCsv("", "", "\n");
            stringBuilder.Append(sentenceText);
        }
        return stringBuilder.ToString();
    }

    private Note GetNoteForPositionInLyrics(int positionInLyrics)
    {
        int position = 0;
        foreach (Sentence sentence in sortedSentences)
        {
            foreach (Note note in sentence.Notes)
            {
                position += note.Text.Length;
                if (position >= positionInLyrics)
                {
                    return note;
                }
            }
            // +1 because of the line break in the text field to separate sentences.
            position += 1;
        }
        return null;
    }
}
