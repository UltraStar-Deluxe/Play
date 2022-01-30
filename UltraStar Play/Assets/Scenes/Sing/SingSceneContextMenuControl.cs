using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using ProTrans;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneContextMenuControl : AbstractContextMenuControl, INeedInjection
{
    [Inject]
    private SingSceneControl singSceneControl;

    public SingSceneContextMenuControl(UIDocument uiDocument, VisualElement targetVisualElement, GameObject gameObject)
        : base(uiDocument, targetVisualElement, gameObject)
    {
    }

    protected override void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_togglePause),
            () => singSceneControl.TogglePlayPause());
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_restart),
            () => singSceneControl.Restart());
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
            () => singSceneControl.SkipToNextSingableNote());
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_exitSong),
            () => singSceneControl.FinishScene(false));
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => singSceneControl.OpenSongInEditor());
    }
}
