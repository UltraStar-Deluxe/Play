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
    public UiNote UiNotePrefab;
    public LyricsDisplayer LyricsDisplayer;

    private AudioSource m_audioSource;

    private SongMeta m_songMeta;
    
    private int m_sentenceIndex;
    private Voice m_voice;
    private Sentence m_sentence;

    private SSingController m_ssingController;

    // The number of lines on which notes can be placed.
    // One can imagine that notes can be placed not only on the drawn lines,
    // but also the rows between two lines.
    public const int NoteLineCount = 16;

    void Start() {
        // Reduced update frequency.
        InvokeRepeating("UpdateCurrentSentence", 0, 0.25f);
    }

    void UpdateCurrentSentence() {
        if(m_songMeta == null || m_voice == null || m_sentence == null) {
            return;
        }

        if(m_ssingController == null) {
            m_ssingController = FindObjectOfType<SSingController>();
        }

        // Change the sentence, when the current beat is over its last note.
        if(m_voice.Sentences.Count > m_sentenceIndex - 1) {
            if((uint)m_ssingController.CurrentBeat > m_sentence.EndBeat) {
                m_sentenceIndex++;
                LoadCurrentSentence();
            } else {
                // Debug.Log("Current beat: "+(uint)m_ssingController.CurrentBeat);
            }
        }
    }

    public void LoadVoice(SongMeta songMeta, string voiceIdentifier) {
        m_songMeta = songMeta;

        string filePath = m_songMeta.Directory + Path.DirectorySeparatorChar + m_songMeta.Filename;
        Debug.Log($"Loading voice of {filePath}");
        var voices = VoicesBuilder.ParseFile(filePath, m_songMeta.Encoding, new List<string>());
        if(string.IsNullOrEmpty(voiceIdentifier)) {
            m_voice = voices.Values.First();
        } else {
            if(!voices.TryGetValue(voiceIdentifier, out m_voice)) {
                throw new Exception($"The song does not contain a voice for {voiceIdentifier}");
            }
        }

        m_sentenceIndex = 0;
        LoadCurrentSentence();
    }

    private void LoadCurrentSentence() {
        if(m_sentenceIndex < m_voice.Sentences.Count) {
            m_sentence = m_voice.Sentences[m_sentenceIndex];
        } else {
            m_sentence = null;
        }

        DisplayCurrentNotes();
        if(LyricsDisplayer != null) {
            LoadCurrentSentenceInLyricsDisplayer();
        }
    }

    private void LoadCurrentSentenceInLyricsDisplayer()
    {
        LyricsDisplayer.SetCurrentSentence(m_sentence);
        if(m_sentenceIndex < m_voice.Sentences.Count - 1) {
            LyricsDisplayer.SetNextSentence(m_voice.Sentences[m_sentenceIndex + 1]);
        } else {
            LyricsDisplayer.SetNextSentence(null);
        }
    }

    private void DisplayCurrentNotes()
    {
        foreach(UiNote uiNote in GetComponentsInChildren<UiNote>()) {
            Destroy(uiNote.gameObject);
        }

        if(m_sentence == null) {
            return;
        }
        
        foreach(var note in m_sentence.Notes) {
            DisplayNote(note);
        }
    }

    private void DisplayNote(Note note)
    {
        UiNote uiNote = Instantiate(UiNotePrefab);
        uiNote.transform.SetParent(transform);

        var uiNoteText = uiNote.GetComponentInChildren<Text>();
        uiNoteText.text = note.Text;

        var beatsInSentence = m_sentence.EndBeat - m_sentence.StartBeat;

        var uiNoteRectTransform = uiNote.GetComponent<RectTransform>();
        var noteLine = note.Pitch % NoteLineCount;
        var anchorY = (double)noteLine / (double)NoteLineCount;
        var anchorX = (double)(note.StartBeat - m_sentence.StartBeat) / (double)beatsInSentence;
        var anchor = new Vector2((float)anchorX, (float)anchorY);
        uiNoteRectTransform.anchorMin = anchor;
        uiNoteRectTransform.anchorMax = anchor;
        uiNoteRectTransform.anchoredPosition = Vector2.zero;

        uiNoteRectTransform.sizeDelta = new Vector2(800 * note.Length / beatsInSentence, uiNoteRectTransform.sizeDelta.y);
    }
}
