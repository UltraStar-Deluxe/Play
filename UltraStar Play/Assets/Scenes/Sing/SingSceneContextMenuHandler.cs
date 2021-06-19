using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneContextMenuHandler : AbstractContextMenuHandler, INeedInjection
{
    [Inject]
    private SingSceneController singSceneController;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_togglePause),
            () => singSceneController.TogglePlayPause());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_restart),
            () => singSceneController.Restart());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
            () => singSceneController.SkipToNextSingableNote());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_exitSong),
            () => singSceneController.FinishScene(false));
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => singSceneController.OpenSongInEditor());
    }
}
