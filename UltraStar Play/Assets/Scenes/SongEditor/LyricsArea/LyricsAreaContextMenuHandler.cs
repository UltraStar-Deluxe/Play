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

public class LyricsAreaContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private LyricsArea lyricsArea;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem("Refresh", () => lyricsArea.UpdateLyrics());
    }


}
