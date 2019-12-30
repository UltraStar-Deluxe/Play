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

public class NoteAreaContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private NoteArea noteArea;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem("Fit vertical", () => noteArea.FitViewportVerticalToNotes());
    }

}
