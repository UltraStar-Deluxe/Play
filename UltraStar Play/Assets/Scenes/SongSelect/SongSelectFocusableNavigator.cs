using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectFocusableNavigator : FocusableNavigator, INeedInjection
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject(UxmlName = R.UxmlNames.topContent)]
    private VisualElement topContent;

    public override void Start()
    {
        base.Start();
        NoNavigationTargetFoundEventStream.Subscribe(evt => OnNoNavigationTargetFound(evt));
        NoSubmitTargetFoundEventStream.Subscribe(evt => OnNoSubmitTargetFound(evt));
    }

    private void OnNoSubmitTargetFound(NoSubmitTargetFoundEvent evt)
    {
        if (GetFocusableNavigatorRootVisualElement() == null)
        {
            songSelectSceneUiControl.CheckAudioAndStartSingScene();
        }
    }

    private void OnNoNavigationTargetFound(NoNavigationTargetFoundEvent evt)
    {
        if (evt.FocusedVisualElement == null
            && IsFocusableNow(lastFocusedVisualElement)
            && GetFocusableNavigatorRootVisualElement(lastFocusedVisualElement) != null
            && !lastFocusedVisualElement.GetAncestors().Contains(topContent))
        {
            FocusLastFocusedVisualElement();
            return;
        }

        if (evt.FocusableNavigatorRootVisualElement == null)
        {
            if (evt.NavigationDirection.y > 0)
            {
                songSelectSceneUiControl.PlaylistChooserControl.FocusDropdownField();
                return;
            }

            if (evt.NavigationDirection.x > 0)
            {
                songRouletteControl.SelectNextSong();
                return;
            }
            if (evt.NavigationDirection.x < 0)
            {
                songRouletteControl.SelectPreviousSong();
                return;
            }
            return;
        }

        if (evt.FocusedVisualElement.GetAncestors().Contains(topContent))
        {
            if (evt.NavigationDirection.y < 0)
            {
                FocusedVisualElement.Blur();
            }
            return;
        }
    }
}
