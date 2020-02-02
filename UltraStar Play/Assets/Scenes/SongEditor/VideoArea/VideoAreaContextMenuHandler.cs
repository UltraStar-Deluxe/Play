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

public class VideoAreaContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SetVideoGapAction setVideoGapAction;

    [Inject]
    private UiManager uiManager;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem("Remove VideoGap", () => RemoveVideoGap());
    }

    private void RemoveVideoGap()
    {
        setVideoGapAction.ExecuteAndNotify(0);
        uiManager.CreateNotification("VideoGap reset to 0");
    }
}
