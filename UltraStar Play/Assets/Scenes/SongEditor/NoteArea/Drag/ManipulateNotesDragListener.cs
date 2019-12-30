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

public class ManipulateNotesDragListener : MonoBehaviour, INeedInjection, INoteAreaDragListener
{
    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    bool isCanceled;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        GameObject raycastTarget = dragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
        EditorUiNote dragStartUiNote = raycastTarget.GetComponent<EditorUiNote>();
        if (dragStartUiNote == null)
        {
            CancelDrag();
            return;
        }

        if (!selectionController.IsSelected(dragStartUiNote.Note))
        {
            selectionController.SetSelection(new List<EditorUiNote> { dragStartUiNote });
        }
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
    }

    public void CancelDrag()
    {
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }
}
