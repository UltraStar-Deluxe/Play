using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class LyricsArea : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    void Start()
    {
        string lyrics = GetLyrics(voices);
        inputField.text = lyrics;
    }

    private string GetLyrics(List<Voice> voices)
    {
        string lyrics = "";
        List<Sentence> sentences = voices.SelectMany(voice => voice.Sentences).ToList();
        sentences.Sort((s1, s2) => s1.StartBeat.CompareTo(s2.StartBeat));
        foreach (Sentence sentence in sentences)
        {
            lyrics += sentence.Notes.Select(note => note.Text).ToCsv("", "", "\n");
        }
        return lyrics;
    }
}
