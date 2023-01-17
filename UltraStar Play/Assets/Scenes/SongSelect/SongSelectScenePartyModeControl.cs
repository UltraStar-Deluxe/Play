using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectScenePartyModeControl : INeedInjection, IInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        previouslyRandomlySelectedSongs = new();
    }
    private static List<SongMeta> previouslyRandomlySelectedSongs = new();

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.gameRoundsOverlay)]
    private VisualElement gameRoundsOverlay;

    [Inject]
    private UiManager uiManager;

    public SongMeta RandomlySelectedSong { get; private set; }

    private MessageDialogControl askToUseJokerControl;

    public void OnInjectionFinished()
    {
        if (songSelectSceneControl.HasPartyModeSettings)
        {
            if (songSelectSceneControl.PartyModeSettings.songSelectionSettings.songSelectionMode == EPartyModeSongSelectionMode.Random)
            {
                SelectRandomSong();
            }

            // Medleys and song queue not supported in party mode
            gameRoundsOverlay.HideByDisplay();
        }
    }

    public void SelectRandomSong()
    {
        RandomlySelectedSong = GetRandomSong();
        songRouletteControl.SelectSong(RandomlySelectedSong);
        Debug.Log($"Selected random song: {RandomlySelectedSong}");
    }

    private VisualElement CreateJokerList()
    {
        VisualElement jokerList = new();
        jokerList.AddToClassList("jokerList");
        if (songSelectSceneControl.SceneData.PartyModeSettings.songSelectionSettings.jokerCount < 0)
        {
            return jokerList;
        }

        for (int i = 0; i < songSelectSceneControl.SceneData.PartyModeSettings.songSelectionSettings.jokerCount; i++)
        {
            MaterialIcon jokerIcon = new();
            jokerIcon.icon = "casino";
            jokerIcon.AddToClassList("jokerIcon");
            jokerList.Add(jokerIcon);
        }

        return jokerList;
    }

    private SongMeta GetRandomSong()
    {
        List<SongMeta> availableSongMetas = playlistManager.GetSongMetas(songSelectSceneControl.PartyModeSettings.songSelectionSettings.songPoolPlaylist);
        if (availableSongMetas.IsNullOrEmpty())
        {
            Debug.LogWarning("No songs available for random song selection. Consider using another playlist or add more songs");
            return null;
        }

        List<SongMeta> unusedSongMetas = availableSongMetas.Except(previouslyRandomlySelectedSongs).ToList();
        if (unusedSongMetas.IsNullOrEmpty())
        {
            previouslyRandomlySelectedSongs = new();
            unusedSongMetas = availableSongMetas.ToList();
        }

        SongMeta randomSongMeta = RandomUtils.RandomOf(unusedSongMetas);
        previouslyRandomlySelectedSongs.Add(randomSongMeta);
        return randomSongMeta;
    }

    public void ReduceJokerCount()
    {
        if (songSelectSceneControl.PartyModeSettings.songSelectionSettings.jokerCount > 0)
        {
            songSelectSceneControl.PartyModeSettings.songSelectionSettings.jokerCount--;
        }
    }

    public void OpenAskToUseJokerDialog(SongMeta songMeta, Action onYes = null)
    {
        CloseAskToUseJokerDialog();
        askToUseJokerControl = uiManager.CreateMessageDialog("Use Joker");
        askToUseJokerControl.Message = $"Use joker to change song?\nJokers left: {GetJokerCountTranslation()}";
        askToUseJokerControl.AddButton(TranslationManager.GetTranslation(R.Messages.yes), () =>
        {
            CloseAskToUseJokerDialog();
            RandomlySelectedSong = songMeta;
            songRouletteControl.SelectSong(songMeta);
            ReduceJokerCount();
            onYes?.Invoke();
        });
        askToUseJokerControl.AddButton(TranslationManager.GetTranslation(R.Messages.no), () =>
        {
            CloseAskToUseJokerDialog();
            songRouletteControl.SelectSong(RandomlySelectedSong);
        });

        askToUseJokerControl.AddVisualElement(CreateJokerList());
    }

    private string GetJokerCountTranslation()
    {
        int jokerCount = songSelectSceneControl.PartyModeSettings.songSelectionSettings.jokerCount;
        if (jokerCount >= 0)
        {
            return jokerCount.ToString();
        }
        else
        {
            return "Unlimited";
        }
    }

    private void CloseAskToUseJokerDialog()
    {
        if (askToUseJokerControl != null)
        {
            askToUseJokerControl.CloseDialog();
            askToUseJokerControl = null;
        }
    }
}
