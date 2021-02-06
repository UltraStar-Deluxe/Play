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

public class SingSceneContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private SingSceneController singSceneController;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_togglePause),
            () => singSceneController.TogglePlayPause());
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_restart),
            () => singSceneController.Restart());
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_skipToNextLyrics),
            () => singSceneController.SkipToNextSingableNote());
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_exitSong),
            () => singSceneController.FinishScene(false));
        contextMenu.AddItem(I18NManager.GetTranslation(R.String.action_openSongEditor),
            () => singSceneController.OpenSongInEditor());
    }
}
