using System;
using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

#pragma warning disable CS0649

public class SongEditorSceneKeyboardController : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSceneController songEditorSceneController;

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private NoteArea noteArea;

    public void Update()
    {
        bool noModifier = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);
        bool ctrlExclusive = Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);
        bool shiftExclusive = !Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);

        bool ctrlShiftExclusive = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);

        int scrollDirection = Math.Sign(Input.mouseScrollDelta.y);

        if (Input.GetKeyUp(KeyCode.Space))
        {
            songEditorSceneController.TogglePlayPause();
        }

        if (scrollDirection != 0 && noteArea.IsPointerOver)
        {
            // Scroll horizontal in NoteArea with mouse wheel
            if (noModifier)
            {
                noteArea.ScrollHorizontal(scrollDirection);
            }

            // Zoom horizontal in NoteArea with Ctrl + mouse wheel
            if (ctrlExclusive)
            {
                noteArea.ZoomHorizontal(scrollDirection);
            }

            // Scroll vertical in NoteArea with Shift + mouse wheel
            if (shiftExclusive)
            {
                noteArea.ScrollVertical(scrollDirection);
            }

            // Zoom vertical in NoteArea with Ctrl + Shift + mouse wheel
            if (ctrlShiftExclusive)
            {
                noteArea.ZoomVertical(scrollDirection);
            }
        }
    }

}
