using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorOctaveOffsetInputField : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongEditorNoteRecorder songEditorNoteRecorder;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    void Start()
    {
        inputField.text = songEditorNoteRecorder.octaveOffset.ToString();
        inputField.OnValueChangedAsObservable().Subscribe(OnTextChanged); ;
    }

    private void OnTextChanged(string newText)
    {
        int newInt = newText.TryParseAsInteger(songEditorNoteRecorder.octaveOffset);
        songEditorNoteRecorder.octaveOffset = newInt;
    }
}
