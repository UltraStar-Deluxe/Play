using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.Text;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LyricsAreaContextMenuControl : ContextMenuControl
{
    [Inject]
    private LyricsAreaControl lyricsAreaControl;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        contextMenu.AddItem("Refresh", () => lyricsAreaControl.UpdateLyrics());
    }
}
