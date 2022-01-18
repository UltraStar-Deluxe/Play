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
    private SingSceneControl singSceneControl;

    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_togglePause),
            () => singSceneControl.TogglePlayPause());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_restart),
            () => singSceneControl.Restart());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
            () => singSceneControl.SkipToNextSingableNote());
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_exitSong),
            () => singSceneControl.FinishScene(false));
        contextMenu.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => singSceneControl.OpenSongInEditor());
    }
}
