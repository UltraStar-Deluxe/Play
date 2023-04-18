using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
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
    private UiManager uiManager;

    [Inject]
    private SongSelectSceneData sceneData;
    
    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;

    public SongMeta RandomlySelectedSong { get; private set; }

    private MessageDialogControl askToUseJokerControl;

    public void OnInjectionFinished()
    {
        UpdatePartyModeSettingsDescription();
    }

    private void UpdatePartyModeSettingsDescription()
    {
        if (!songSelectSceneControl.HasPartyModeSceneData)
        {
            return;
        }

        GameRoundSettings currentRoundSettings = settings.GameRoundSettings;
        GameRoundFinishConditionSettings finishConditionSettings = currentRoundSettings.finishConditionSettings;

        string GetFinishConditionDescription()
        {
            if (finishConditionSettings == null)
            {
                return "";
            }

            if (finishConditionSettings.condition == EGameRoundFinishCondition.ReachPoints)
            {
                return $"Reach {finishConditionSettings.points} points";
            }
            else if (finishConditionSettings.condition == EGameRoundFinishCondition.ReachAdvanceOfPoints)
            {
                return $"Reach advance of {finishConditionSettings.points} points";
            }

            return "";
        }

        string GetUnconditionalModifierDescription()
        {
            if (currentRoundSettings.UnconditionalModifiers.IsNullOrEmpty())
            {
                return "";
            }
            string modifierCsv = currentRoundSettings.UnconditionalModifiers.ToList()
                .OrderBy(it => it.ToString())
                .JoinWith(", ");
            return modifierCsv;
        }

        string GetConditionalModifierDescription()
        {
            if (currentRoundSettings.ConditionalModifiers.IsNullOrEmpty())
            {
                return "";
            }
            string modifierCsv = currentRoundSettings.ConditionalModifiers.ToList()
                .OrderBy(it => it.ToString())
                .JoinWith(", ");
            string modifierConditionDescription = GameRoundSettingsUtils.GetModifierConditionDescription(currentRoundSettings);
            return $"{modifierCsv} {modifierConditionDescription}";
        }
    }

    public void SelectRandomSong()
    {
        RandomlySelectedSong = GetRandomSong();
        songRouletteControl.SelectSong(RandomlySelectedSong);
        sceneData.SongMeta = RandomlySelectedSong;
        Debug.Log($"Selected random song: {RandomlySelectedSong}");
    }

    private VisualElement CreateJokerList()
    {
        VisualElement jokerList = new();
        jokerList.AddToClassList("jokerList");
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
        if (songSelectSceneControl.PartyModeSceneData.remainingJokerCount > 0)
        {
            songSelectSceneControl.PartyModeSceneData.remainingJokerCount--;
        }
    }

    public void OpenAskToUseJokerDialog(SongMeta songMeta, Action onYes = null)
    {
        CloseAskToUseJokerDialog();
        askToUseJokerControl = uiManager.CreateDialogControl("Use Joker");
        askToUseJokerControl.Message = $"Use joker to change song?\nJokers left: {GetJokerCountTranslation()}";
        askToUseJokerControl.AddButton(TranslationManager.GetTranslation(R.Messages.yes), _ =>
        {
            CloseAskToUseJokerDialog();
            RandomlySelectedSong = songMeta;
            songRouletteControl.SelectSong(songMeta);
            ReduceJokerCount();
            onYes?.Invoke();
        });
        askToUseJokerControl.AddButton(TranslationManager.GetTranslation(R.Messages.no), _ =>
        {
            CloseAskToUseJokerDialog();
            songRouletteControl.SelectSong(RandomlySelectedSong);
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
