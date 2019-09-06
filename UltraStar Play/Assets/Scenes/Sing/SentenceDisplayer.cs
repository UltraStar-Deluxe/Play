using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SentenceDisplayer : MonoBehaviour
{
    // The number of lines on which notes can be placed.
    // One can imagine that notes can be placed not only on the drawn lines,
    // but also the rows between two lines.
    public const int NoteLineCount = 16;

    public UiNote uiNotePrefab;

    private SongMeta songMeta;

    private int sentenceIndex;
    private Voice voice;
    private Sentence sentence;

    private SingSceneController singSceneController;

    public LyricsDisplayer LyricsDisplayer { get; set; }

    void Start()
    {
        // Reduced update frequency.
        InvokeRepeating("UpdateCurrentSentence", 0, 0.25f);
    }

    void UpdateCurrentSentence()
    {
        if (songMeta == null || voice == null || sentence == null)
        {
            return;
        }

        if (singSceneController == null)
        {
            singSceneController = FindObjectOfType<SingSceneController>();
        }

        // Change the sentence, when the current beat is over its last note.
        if (voice.Sentences.Count > sentenceIndex - 1)
        {
            if ((uint)singSceneController.CurrentBeat > sentence.EndBeat)
            {
                sentenceIndex++;
                LoadCurrentSentence();
            }
            else
            {
                // Debug.Log("Current beat: "+(uint)m_ssingController.CurrentBeat);
            }
        }
    }

    public void LoadVoice(SongMeta songMeta, string voiceIdentifier)
    {
        this.songMeta = songMeta;

        string filePath = this.songMeta.Directory + Path.DirectorySeparatorChar + this.songMeta.Filename;
        Debug.Log($"Loading voice of {filePath}");
        Dictionary<string, Voice> voices = VoicesBuilder.ParseFile(filePath, this.songMeta.Encoding, new List<string>());
        if (string.IsNullOrEmpty(voiceIdentifier))
        {
            voice = voices.Values.First();
        }
        else
        {
            if (!voices.TryGetValue(voiceIdentifier, out voice))
            {
                throw new Exception($"The song does not contain a voice for {voiceIdentifier}");
            }
        }

        sentenceIndex = 0;
        LoadCurrentSentence();
    }

    private void LoadCurrentSentence()
    {
        if (sentenceIndex < voice.Sentences.Count)
        {
            sentence = voice.Sentences[sentenceIndex];
        }
        else
        {
            sentence = null;
        }

        DisplayCurrentNotes();
        if (LyricsDisplayer != null)
        {
            LoadCurrentSentenceInLyricsDisplayer();
        }
    }

    private void LoadCurrentSentenceInLyricsDisplayer()
    {
        LyricsDisplayer.SetCurrentSentence(sentence);
        if (sentenceIndex < voice.Sentences.Count - 1)
        {
            LyricsDisplayer.SetNextSentence(voice.Sentences[sentenceIndex + 1]);
        }
        else
        {
            LyricsDisplayer.SetNextSentence(null);
        }
    }

    private void DisplayCurrentNotes()
    {
        foreach (UiNote uiNote in GetComponentsInChildren<UiNote>())
        {
            Destroy(uiNote.gameObject);
        }

        if (sentence == null)
        {
            return;
        }

        foreach (Note note in sentence.Notes)
        {
            DisplayNote(note);
        }
    }

    private void DisplayNote(Note note)
    {
        UiNote uiNote = Instantiate(uiNotePrefab);
        uiNote.transform.SetParent(transform);

        Text uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = note.Text;

        uint beatsInSentence = sentence.EndBeat - sentence.StartBeat;

        RectTransform uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        int noteLine = note.Pitch % NoteLineCount;
        double anchorY = (double)noteLine / (double)NoteLineCount;
        double anchorX = (double)(note.StartBeat - sentence.StartBeat) / (double)beatsInSentence;
        Vector2 anchor = new Vector2((float)anchorX, (float)anchorY);
        uiNoteRectTransform.anchorMin = anchor;
        uiNoteRectTransform.anchorMax = anchor;
        uiNoteRectTransform.anchoredPosition = Vector2.zero;

        uiNoteRectTransform.sizeDelta = new Vector2(800 * note.Length / beatsInSentence, uiNoteRectTransform.sizeDelta.y);
    }
}
