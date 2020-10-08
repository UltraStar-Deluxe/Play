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

public class SpaceBetweenNotesButton : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public InputField numberOfBeatsInputField;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Button button;

    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private SongMeta songMeta;

    private void Start()
    {
        button.OnClickAsObservable().Subscribe(_ =>
        {
            if (int.TryParse(numberOfBeatsInputField.text, out int spaceInBeats))
            {
                List<Note> selectedNotes = selectionController.GetSelectedNotes();
                if (selectedNotes.IsNullOrEmpty())
                {
                    // Perform on all notes, but per voice
                    songMeta.GetVoices()
                        .ForEach(voice => spaceBetweenNotesAction.ExecuteAndNotify(SongMetaUtils.GetAllNotes(voice), spaceInBeats));
                }
                else
                {
                    spaceBetweenNotesAction.ExecuteAndNotify(selectedNotes, spaceInBeats);
                }
            }
        });
    }
}
