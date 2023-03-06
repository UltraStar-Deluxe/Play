using System;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectFocusableNavigator : FocusableNavigator
{
    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;
    
    [Inject(UxmlName = R.UxmlNames.topContent)]
    private VisualElement topContent;

    [Inject(UxmlName = R.UxmlNames.menuButton)]
    private VisualElement menuButton;

    private void Awake()
    {
        SetOtherFocusableNavigatorsActive(false);
    }

    private void SetOtherFocusableNavigatorsActive(bool value)
    {
        FindObjectsOfType<FocusableNavigator>(true)
            .Where(it => it != this)
            .ForEach(it => it.gameObject.SetActive(value));
    }

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        NoNavigationTargetFoundEventStream.Subscribe(evt => OnNoNavigationTargetFound(evt));
        NoSubmitTargetFoundEventStream.Subscribe(_ => OnNoSubmitTargetFound());
        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ => SetOtherFocusableNavigatorsActive(true))
            .AddTo(gameObject);
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
