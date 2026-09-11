using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Inject]
    private DialogManager dialogManager;

    [Inject]
    private SongSelectSceneData sceneData;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;

    public SongMeta RandomlySelectedSong { get; private set; }

    private MessageDialogControl askToUseJokerControl;

    public void OnInjectionFinished()
    {
    }

    public void SelectRandomSong()
    {
        RandomlySelectedSong = GetRandomSong();
        songRouletteControl.SelectEntryBySongMeta(RandomlySelectedSong);
        sceneData.SongMeta = RandomlySelectedSong;
        Debug.Log($"Selected random song: {RandomlySelectedSong}");
    }

    private VisualElement CreateJokerList()
    {
        VisualElement jokerList = new();
        jokerList.name = "jokerList";
        if (sceneData.partyModeSceneData.remainingJokerCount < 0)
        {
            return jokerList;
        }

        for (int i = 0; i < sceneData.partyModeSceneData.remainingJokerCount; i++)
        {
            MaterialIcon jokerIcon = new();
            jokerIcon.Icon = "casino";
            jokerIcon.AddToClassList("jokerIcon");
            jokerList.Add(jokerIcon);
        }

        return jokerList;
    }

    private SongMeta GetRandomSong()
    {
        List<SongMeta> availableSongMetas = playlistManager.GetSongMetas(songSelectSceneControl.PartyModeSettings.SongSelectionSettings.SongPoolPlaylist);
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
        if (songSelectSceneControl.PartyModeSceneData.remainingJokerCount > 0)
        {
            songSelectSceneControl.PartyModeSceneData.remainingJokerCount--;
        }
    }

    public void OpenAskToUseJokerDialog(SongMeta songMeta, Action onYes = null)
    {
        CloseAskToUseJokerDialog();
        askToUseJokerControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_useJokerDialog_title));
        askToUseJokerControl.Message = Translation.Get(R.Messages.songSelectScene_useJokerDialog_message, "jokerCount", GetJokerCountTranslation());
        askToUseJokerControl.AddButton(Translation.Get(R.Messages.common_yes), _ =>
        {
            CloseAskToUseJokerDialog();
            RandomlySelectedSong = songMeta;
            songRouletteControl.SelectEntryBySongMeta(songMeta);
            ReduceJokerCount();
            onYes?.Invoke();
        });
        askToUseJokerControl.AddButton(Translation.Get(R.Messages.common_no), _ =>
        {
            CloseAskToUseJokerDialog();
            songRouletteControl.SelectEntryBySongMeta(RandomlySelectedSong);
        });

        askToUseJokerControl.AddVisualElement(CreateJokerList());
    }

    private string GetJokerCountTranslation()
    {
        int jokerCount = songSelectSceneControl.PartyModeSceneData.remainingJokerCount;
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
