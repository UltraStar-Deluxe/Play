using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PositionInLyricsIndicator : MonoBehaviour
{
    private const int CanvasWidth = 800;

    public LyricsDisplayer lyricsDisplayer;

    private SingSceneController singSceneController;

    private double velocityPerSecond;

    private Sentence lastSentence;

    private Sentence CurrentSentence
    {
        get
        {
            return lyricsDisplayer.CurrentSentence;
        }
    }

    private Text CurrentSentenceText
    {
        get
        {
            return lyricsDisplayer.currentSentenceText;
        }
    }

    private SongMeta SongMeta
    {
        get
        {
            return singSceneController.SongMeta;
        }
    }

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        singSceneController = FindObjectOfType<SingSceneController>();
    }

    void Update()
    {
        if (lastSentence != CurrentSentence)
        {
            lastSentence = CurrentSentence;
            Reset();
        }
        else
        {
            var step = (float)velocityPerSecond * Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + step, rectTransform.anchoredPosition.y);
        }
        CalculateVelocity();
    }

    public void Reset()
    {
        MoveToLeftSideOfScreen();
        velocityPerSecond = 0;
    }

    private void MoveToLeftSideOfScreen()
    {
        rectTransform.anchoredPosition = new Vector2(-CanvasWidth / 2.0f, rectTransform.anchoredPosition.y);
    }

    private void CalculateVelocity()
    {
        if (CurrentSentence == null
            || CurrentSentenceText.text.Length == 0
            || CurrentSentenceText.cachedTextGenerator.vertexCount == 0)
        {
            return;
        }
        double positionInSongInSeconds = singSceneController.PositionInSongInSeconds;

        double currentBeat = singSceneController.CurrentBeat;
        double sentenceStartBeat = CurrentSentence.StartBeat;
        double sentenceEndBeat = CurrentSentence.EndBeat;

        double positionInSentenceInSeconds = positionInSongInSeconds - (SongMeta.Gap / 1000.0f);
        double sentenceStartInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, sentenceStartBeat);
        double sentenceEndInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, sentenceEndBeat);

        double positionIndicatorStartInSeconds = sentenceStartInSeconds - 2.0f;

        if (positionInSentenceInSeconds >= positionIndicatorStartInSeconds)
        {
            double endPos = double.MinValue;
            double endTimeInSeconds = 0f;

            if (positionInSentenceInSeconds <= sentenceStartInSeconds)
            {
                // Range before first note of sentence.
                double sentenceFirstCharacterPosition = GetStartPositionOfNote(CurrentSentenceText, CurrentSentence, CurrentSentence.Notes[0]);
                endPos = sentenceFirstCharacterPosition;
                endTimeInSeconds = sentenceStartInSeconds;
            }
            else if (positionInSentenceInSeconds <= sentenceEndInSeconds)
            {
                // Range inside sentence.
                Note currentNote = GetCurrentOrNextNote(currentBeat);
                if (currentNote != null)
                {
                    double noteEndInSeconds = BpmUtils.BeatToSecondsInSong(SongMeta, currentNote.EndBeat);
                    endPos = GetEndPositionOfNote(CurrentSentenceText, CurrentSentence, currentNote);
                    endTimeInSeconds = noteEndInSeconds;
                }
            }
            double remainingTime = endTimeInSeconds - positionInSentenceInSeconds;
            if (endPos > double.MinValue)
            {
                if (remainingTime > 0 && endPos > rectTransform.anchoredPosition.x)
                {
                    velocityPerSecond = (endPos - rectTransform.anchoredPosition.x) / remainingTime;
                }
                else
                {
                    rectTransform.anchoredPosition = new Vector2((float)endPos, rectTransform.anchoredPosition.y);
                }
            }
        }
    }

    private float GetEndPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        List<Note> noteAndNotesBefore = sentence.Notes.ElementsBefore(note, true);
        int countNonWhitespaceChars = noteAndNotesBefore.Select(it => it.Text.Replace(" ", "").Length).Sum();
        Vector3 pos = GetRightPositionOfCharacter(currentSentenceText, countNonWhitespaceChars - 1);
        return pos.x;
    }

    private float GetStartPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        List<Note> notesBefore = sentence.Notes.ElementsBefore(note, false);
        int countNonWhitespaceChars = notesBefore.Select(it => it.Text.Replace(" ", "").Length).Sum();
        Vector3 pos = GetLeftPositionOfCharacter(currentSentenceText, countNonWhitespaceChars);
        return pos.x;
    }

    private Note GetCurrentOrNextNote(double currentBeat)
    {
        Note note = CurrentSentence.Notes
            .Where(it => (currentBeat <= it.EndBeat)).FirstOrDefault();
        return note;
    }

    private Vector3 GetLeftPositionOfCharacter(Text text, int charIndex)
    {
        // Use position of a vertex on the left side of the character.
        int vertIndex = charIndex * 4;
        UIVertex vertexOfCharacter = text.cachedTextGenerator.verts[vertIndex];
        Vector3 positionOfVertex = vertexOfCharacter.position;
        return positionOfVertex;
    }

    private Vector3 GetRightPositionOfCharacter(Text text, int charIndex)
    {
        // Use position of a vertex on the right side of the character.
        int vertIndex = ((charIndex + 1) * 4) - 3;
        UIVertex vertexOfCharacter = text.cachedTextGenerator.verts[vertIndex];
        Vector3 positionOfVertex = vertexOfCharacter.position;
        return positionOfVertex;
    }
}
