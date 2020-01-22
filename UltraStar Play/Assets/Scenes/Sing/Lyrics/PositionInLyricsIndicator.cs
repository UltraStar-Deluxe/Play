using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PositionInLyricsIndicator : MonoBehaviour
{
    // The reference size of the canvas
    private float canvasWidth;

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

    private List<Note> SortedNotes
    {
        get
        {
            return lyricsDisplayer.SortedNotes;
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

        // Get the canvas width.
        Canvas canvas = CanvasUtils.FindCanvas();
        RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
        canvasWidth = canvasRectTransform.rect.width;
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
            float step = (float)velocityPerSecond * Time.deltaTime;
            rectTransform.position = new Vector3(rectTransform.position.x + step, rectTransform.position.y, rectTransform.position.z);
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
        rectTransform.position = new Vector3(0, rectTransform.position.y, rectTransform.position.z);
    }

    private void CalculateVelocity()
    {
        if (CurrentSentence == null
            || CurrentSentenceText.text.Length == 0
            || CurrentSentenceText.cachedTextGenerator.vertexCount == 0)
        {
            return;
        }
        double positionInSongInMillis = singSceneController.PositionInSongInMillis;

        double currentBeat = singSceneController.CurrentBeat;
        double sentenceMinBeat = CurrentSentence.MinBeat;
        double sentenceMaxBeat = CurrentSentence.MaxBeat;

        double positionInSentenceInMillis = positionInSongInMillis - SongMeta.Gap;
        double sentenceStartInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(SongMeta, sentenceMinBeat);
        double sentenceEndInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(SongMeta, sentenceMaxBeat);

        double positionIndicatorStartInMillis = sentenceStartInMillis - 2000;

        if (positionInSentenceInMillis >= positionIndicatorStartInMillis)
        {
            double endPos = double.MinValue;
            double endTimeInMillis = 0f;

            if (positionInSentenceInMillis <= sentenceStartInMillis)
            {
                // Range before first note of sentence.
                double sentenceFirstCharacterPosition = GetStartPositionOfNote(CurrentSentenceText, CurrentSentence, SortedNotes[0]);
                endPos = sentenceFirstCharacterPosition;
                endTimeInMillis = sentenceStartInMillis;
            }
            else if (positionInSentenceInMillis <= sentenceEndInMillis)
            {
                // Range inside sentence.
                Note currentNote = GetCurrentOrNextNote(currentBeat);
                if (currentNote != null)
                {
                    double noteEndInMillis = BpmUtils.BeatToMillisecondsInSongWithoutGap(SongMeta, currentNote.EndBeat);
                    endPos = GetEndPositionOfNote(CurrentSentenceText, CurrentSentence, currentNote);
                    endTimeInMillis = noteEndInMillis;
                }
            }

            if (endPos > double.MinValue)
            {
                // The position of the character is returned in absolute pixels,
                // where the left side is (-Screen.width/2) and the right side is (+Screen.width/2).
                // But, the world position of the RectTransform is ranging from 0 to Screen.width
                double endPosForRectTransform = endPos + Screen.width / 2.0;
                double remainingTimeInMillis = endTimeInMillis - positionInSentenceInMillis;
                if (remainingTimeInMillis > 0 && endPosForRectTransform > rectTransform.position.x)
                {
                    double remainingTimeInSeconds = remainingTimeInMillis / 1000;
                    velocityPerSecond = (endPosForRectTransform - rectTransform.position.x) / remainingTimeInSeconds;
                }
                else
                {
                    velocityPerSecond = 0;
                    rectTransform.position = new Vector3((float)endPosForRectTransform, rectTransform.position.y, rectTransform.position.z);
                }
            }
        }
    }

    private float GetEndPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        List<Note> noteAndNotesBefore = sentence.Notes.GetElementsBefore(note, true);
        int countNonWhitespaceChars = noteAndNotesBefore.Select(it => it.Text.Replace(" ", "").Length).Sum();
        Vector3 pos = GetRightPositionOfCharacter(currentSentenceText, countNonWhitespaceChars - 1);
        return pos.x;
    }

    private float GetStartPositionOfNote(Text currentSentenceText, Sentence sentence, Note note)
    {
        List<Note> notesBefore = sentence.Notes.GetElementsBefore(note, false);
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
