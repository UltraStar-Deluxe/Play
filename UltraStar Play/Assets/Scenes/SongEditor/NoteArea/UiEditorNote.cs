using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

#pragma warning disable CS0649

public class UiEditorNote : MonoBehaviour, IPointerClickHandler
{
    [InjectedInInspector]
    public EditorNoteLyricsInputField lyricsInputFieldPrefab;

    [InjectedInInspector]
    public Image goldenNoteImageOverlay;

    [InjectedInInspector]
    public Image backgroundImage;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    [Inject]
    private Injector injector;

    private EditorNoteLyricsInputField activeLyricsInputField;

    public Note Note { get; private set; }

    public void Init(Note note)
    {
        this.Note = note;
        SetText(note.Text);
        goldenNoteImageOverlay.gameObject.SetActive(note.IsGolden);
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                     ped.position,
                                                                     ped.pressEventCamera,
                                                                     out Vector2 localPoint))
        {
            return;
        }

        if (ped.clickCount == 1)
        {
            // Move the playback position to the start of the note
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, Note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
        else if (ped.clickCount == 2)
        {
            StartEditingNoteText();
        }
    }

    public void SetText(string newText)
    {
        if (Note.IsFreestyle)
        {
            uiText.text = $"<i>{newText}</i>";
        }
        else
        {
            uiText.text = newText;
        }
    }

    private void StartEditingNoteText()
    {
        if (activeLyricsInputField != null)
        {
            return;
        }

        activeLyricsInputField = Instantiate(lyricsInputFieldPrefab, transform);
        injector.Inject(activeLyricsInputField);
        activeLyricsInputField.Init(this, Note.Text);
    }
}
