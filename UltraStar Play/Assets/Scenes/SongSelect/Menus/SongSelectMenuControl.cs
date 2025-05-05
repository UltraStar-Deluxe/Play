using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMenuControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Settings settings;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneData sceneData;

    [Inject(UxmlName = R.UxmlNames.showScrollBarInSongSelectToggle)]
    private Toggle showScrollBarInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.showSongIndexInSongSelectToggle)]
    private Toggle showSongIndexInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.navigateFoldersInSongSelectToggle)]
    private Toggle navigateFoldersInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.closeSongSelectSceneMenuButton2)]
    private Button closeSongSelectSceneMenuButton2;

    [Inject(UxmlName = R.UxmlNames.applySongSelectOptionsButton)]
    private Button applySongSelectOptionsButton;

    [Inject(UxmlName = R.UxmlNames.sceneMenuOverlay)]
    private VisualElement sceneMenuOverlay;

    [Inject(UxmlName = R.UxmlNames.toggleSceneMenuButton)]
    private Button toggleSceneMenuButton;

    [Inject(UxmlName = R.UxmlNames.closeSongSelectSceneMenuButton)]
    private Button closeSongSelectSceneMenuButton;

    [Inject(UxmlName = R.UxmlNames.hiddenHideSceneMenuOverlayArea)]
    private VisualElement hiddenHideSceneMenuOverlayArea;

    public VisualElementSlideInControl SceneMenuSlideInControl { get; private set; }

    public SongMeta SelectedSong => (songRouletteControl.SelectedEntry as SongSelectSongEntry)?.SongMeta;

    public void OnInjectionFinished()
    {
        FieldBindingUtils.Bind(showScrollBarInSongSelectToggle,
            () => settings.ShowScrollBarInSongSelect,
            newValue => settings.ShowScrollBarInSongSelect = newValue);

        FieldBindingUtils.Bind(showSongIndexInSongSelectToggle,
            () => settings.ShowSongIndexInSongSelect,
            newValue => settings.ShowSongIndexInSongSelect = newValue);

        FieldBindingUtils.Bind(navigateFoldersInSongSelectToggle,
            () => settings.NavigateByFoldersInSongSelect,
            newValue => settings.NavigateByFoldersInSongSelect = newValue);

        applySongSelectOptionsButton.RegisterCallbackButtonTriggered(_ => ReloadScene());
        closeSongSelectSceneMenuButton2.RegisterCallbackButtonTriggered(_ => SceneMenuSlideInControl.SlideOut());

        SceneMenuSlideInControl = new(sceneMenuOverlay, ESide2D.Right, false);
        SongSelectSlideInControlUtils.InitSlideInControl(SceneMenuSlideInControl, toggleSceneMenuButton, closeSongSelectSceneMenuButton, sceneMenuOverlay, hiddenHideSceneMenuOverlayArea);
    }

    private void ReloadScene()
    {
        sceneNavigator.LoadScene(EScene.SongSelectScene, new SongSelectSceneData()
        {
            SongMeta = SelectedSong,
            partyModeSceneData = sceneData.partyModeSceneData,
        });
    }
}
