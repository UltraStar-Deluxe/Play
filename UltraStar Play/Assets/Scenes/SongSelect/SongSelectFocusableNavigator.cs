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
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject(UxmlName = R.UxmlNames.topContent)]
    private VisualElement topContent;

    [Inject(UxmlName = R.UxmlNames.menuButton)]
    private VisualElement menuButton;

    private void Awake()
    {
        // Disable default FocusableNavigator
        FindObjectsOfType<FocusableNavigator>()
            .Where(it => it != this)
            .ForEach(it => it.gameObject.SetActive(false));
    }

    public override void Start()
    {
        base.Start();
        NoNavigationTargetFoundEventStream.Subscribe(evt => OnNoNavigationTargetFound(evt));
        NoSubmitTargetFoundEventStream.Subscribe(_ => OnNoSubmitTargetFound());
    }

    private void OnNoSubmitTargetFound()
    {
        if (GetFocusableNavigatorRootVisualElement() == null)
        {
            songSelectSceneControl.CheckAudioAndStartSingScene();
        }
    }

    public override void OnNavigate(Vector2 navigationDirection)
    {
        if (navigationDirection.y < 0
            && FocusedVisualElement != menuButton
            && GetFocusableNavigatorRootVisualElement() == topContent)
        {
            FocusSongRoulette();
            return;
        }

        base.OnNavigate(navigationDirection);
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
                songSelectSceneControl.PlaylistChooserControl.FocusPlaylistChooser();
                return;
            }
            if (evt.NavigationDirection.y < 0)
            {
                songSelectSceneControl.ToggleSelectedSongIsFavorite();
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
                FocusSongRoulette();
            }
        }
    }

    public void FocusSongRoulette()
    {
        // SongRoulette has focus when nothing else has focus.
        FocusedVisualElement?.Blur();
    }
}
