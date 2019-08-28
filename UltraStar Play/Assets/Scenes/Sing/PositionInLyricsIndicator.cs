using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PositionInLyricsIndicator : MonoBehaviour
{
    public LyricsDisplayer LyricsDisplayer;

    private SSingController m_ssingController;

    private double velocityPerSecond;

    private Sentence m_lastSentence;

    private Sentence CurrentSentence {
        get {
            return LyricsDisplayer.CurrentSentence;
        }
    }

    private Text CurrentSentenceText {
        get {
            return LyricsDisplayer.CurrentSentenceText;
        }
    }

    private SongMeta SongMeta {
        get {
            return m_ssingController.SongMeta;
        }
    }

    private RectTransform m_rectTransform;

    private const int canvasWidth = 800;

    void Start() {
        m_rectTransform = GetComponent<RectTransform>();
        m_ssingController = FindObjectOfType<SSingController>();
    }

    void Update() {
        if(m_lastSentence != CurrentSentence) {
            m_lastSentence = CurrentSentence;
            Reset();
        } else {
            var step = (float)velocityPerSecond * Time.deltaTime;
            m_rectTransform.anchoredPosition = new Vector2(m_rectTransform.anchoredPosition.x + step, m_rectTransform.anchoredPosition.y);
        }
        CalculateVelocity();
    }

    public void Reset() {
        MoveToLeftSideOfScreen();
        velocityPerSecond = 0;
    }

    private void MoveToLeftSideOfScreen() {
        m_rectTransform.anchoredPosition = new Vector2(-canvasWidth / 2.0f, m_rectTransform.anchoredPosition.y);
    }

    private void CalculateVelocity() {
        if(CurrentSentence == null
            || CurrentSentenceText.text.Length == 0
            || CurrentSentenceText.cachedTextGenerator.vertexCount == 0) {
            return;
        }
        var positionInSongInSeconds = m_ssingController.PositionInSongInSeconds;

        var currentBeat = m_ssingController.CurrentBeat;
        var sentenceStartBeat = CurrentSentence.StartBeat;
        var sentenceEndBeat = CurrentSentence.EndBeat;

        var positionInSentenceInSeconds = positionInSongInSeconds - (SongMeta.Gap / 1000.0f);
        var sentenceStartInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, sentenceStartBeat);
        var sentenceEndInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, sentenceEndBeat);

        var positionIndicatorStartInSeconds = sentenceStartInSeconds - 2.0f;

        if (positionInSentenceInSeconds >= positionIndicatorStartInSeconds) {
            var endPos = float.MinValue;
            var endTimeInSeconds = 0f;

            if(positionInSentenceInSeconds <= sentenceStartInSeconds) {
                // Range before first note of sentence.
                var sentenceFirstCharacterPosition = GetStartPositionOfNote(CurrentSentenceText, CurrentSentence, CurrentSentence.Notes[0]);
                endPos = sentenceFirstCharacterPosition;
                endTimeInSeconds = sentenceStartInSeconds;
            } else if (positionInSentenceInSeconds <= sentenceEndInSeconds) {
                // Range inside sentence.
                var currentNote = GetCurrentOrNextNote(currentBeat);
                if (currentNote != null) {
                    var noteEndInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, currentNote.EndBeat);
                    endPos = GetEndPositionOfNote(CurrentSentenceText, CurrentSentence, currentNote);
                    endTimeInSeconds = noteEndInSeconds;
                }
            }
            var remainingTime = endTimeInSeconds - positionInSentenceInSeconds;
            if(endPos > float.MinValue) {
                if(remainingTime > 0) {
                    velocityPerSecond = (endPos - m_rectTransform.anchoredPosition.x) / remainingTime;
                } else {
                    m_rectTransform.anchoredPosition = new Vector2(endPos, m_rectTransform.anchoredPosition.y);
                }
            }
        }
    }

    private float GetEndPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        var noteAndNotesBefore = sentence.Notes.ElementsBefore(note, true);
        var countNonWhitespaceChars = noteAndNotesBefore.Select(it => it.Text.Replace(" ", "").Length).Sum();
        var pos = GetRightPositionOfCharacter(currentSentenceText, countNonWhitespaceChars - 1);
        return pos.x;
    }

    private float GetStartPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        var notesBefore = sentence.Notes.ElementsBefore(note, false);
        var countNonWhitespaceChars = notesBefore.Select(it => it.Text.Replace(" ", "").Length).Sum();
        var pos = GetLeftPositionOfCharacter(currentSentenceText, countNonWhitespaceChars);
        return pos.x;
    }

    private Note GetCurrentOrNextNote(double currentBeat) {
        var note = CurrentSentence.Notes
            .Where( it => (currentBeat <= it.EndBeat) ).FirstOrDefault();
        return note;
    }

    private Vector3 GetLeftPositionOfCharacter(Text text, int charIndex) {
        // Use position of a vertex on the left side of the character.
        var vertIndex = charIndex * 4;
        var vertexOfCharacter = text.cachedTextGenerator.verts[ vertIndex ];
        var positionOfVertex = vertexOfCharacter.position;
        return positionOfVertex;
    }

    private Vector3 GetRightPositionOfCharacter(Text text, int charIndex) {
        // Use position of a vertex on the right side of the character.
        var vertIndex = ((charIndex + 1) * 4) - 3; 
        var vertexOfCharacter = text.cachedTextGenerator.verts[ vertIndex ];
        var positionOfVertex = vertexOfCharacter.position;
        return positionOfVertex;
    }
}
