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

    [Inject(UxmlName = R.UxmlNames.gameRoundsOverlay)]
    private VisualElement gameRoundsOverlay;

    [Inject(UxmlName = R.UxmlNames.partySettingsContainer)]
    private VisualElement partySettingsContainer;

    [Inject(UxmlName = R.UxmlNames.finishConditionContainer)]
    private VisualElement finishConditionContainer;

    [Inject(UxmlName = R.UxmlNames.modifierConditionContainer)]
    private VisualElement modifierConditionContainer;

    [Inject(UxmlName = R.UxmlNames.finishConditionDescription)]
    private Label finishConditionDescription;

    [Inject(UxmlName = R.UxmlNames.modifierDescription)]
    private Label modifierDescription;

    public SongMeta RandomlySelectedSong { get; private set; }

    private MessageDialogControl askToUseJokerControl;

    public void OnInjectionFinished()
    {
        UpdatePartyModeSettingsDescription();
        if (songSelectSceneControl.HasPartyModeSceneData)
        {
            // Medleys and song queue not supported in party mode
            gameRoundsOverlay.HideByDisplay();
        }
    }

    private void UpdatePartyModeSettingsDescription()
    {
        partySettingsContainer.SetVisibleByDisplay(songSelectSceneControl.HasPartyModeSceneData);
        if (!songSelectSceneControl.HasPartyModeSceneData)
        {
            return;
        }

        GameRoundSettings currentRoundSettings = songSelectSceneControl.PartyModeSceneData.CurrentRoundSettings;
        GameRoundFinishConditionSettings finishConditionSettings = currentRoundSettings.finishConditionSettings;
        HashSet<EGameRoundModifier> modifiers = currentRoundSettings.modifiers;
        GameRoundModifierConditionSettings modifierConditionSettings = currentRoundSettings.modifierConditionSettings;

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

        string GetModifierConditionDescription()
        {
            if (modifiers.IsNullOrEmpty()
                || modifierConditionSettings == null
                || modifierConditionSettings.condition == EGameRoundModifierCondition.Always)
            {
                return "";
            }
            else if (modifierConditionSettings.condition == EGameRoundModifierCondition.PlayerAdvance)
            {
                if (modifierConditionSettings.scoreFrom <= 0)
                {
                    return "";
                }
                return $"when player has advance of {modifierConditionSettings.scoreFrom} points";
            }
            else if (modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
            {
                if (modifierConditionSettings.scoreFrom <= 0 && modifierConditionSettings.scoreUntil >= 10000)
                {
                    return "";
                }
                return $"when score is between {modifierConditionSettings.scoreFrom} and {modifierConditionSettings.scoreUntil}";
            }
            else if (modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
            {
                if (modifierConditionSettings.timeFrom <= 0 && modifierConditionSettings.timeUntil >= 100)
                {
                    return "";
                }
                return $"when time is between {modifierConditionSettings.timeFrom}% and {modifierConditionSettings.timeUntil}%";
            }

            return "";
        }

        string GetModifierDescription()
        {
            if (modifiers.IsNullOrEmpty())
            {
                return "";
            }
            string modifierCsv = modifiers.ToList()
                .OrderBy(it => it.ToString())
                .JoinWith(", ");
            string modifierConditionDescription = GetModifierConditionDescription();
            return $"{modifierCsv} {modifierConditionDescription}";
        }

        finishConditionDescription.text = GetFinishConditionDescription();
        finishConditionContainer.SetVisibleByDisplay(!finishConditionDescription.text.IsNullOrEmpty());
        modifierDescription.text = GetModifierDescription();
        modifierConditionContainer.SetVisibleByDisplay(!modifierDescription.text.IsNullOrEmpty());
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
        if (sceneData.partyModeSceneData.PartyModeSettings.songSelectionSettings.jokerCount < 0)
        {
            return jokerList;
        }

        for (int i = 0; i < sceneData.partyModeSceneData.PartyModeSettings.songSelectionSettings.jokerCount; i++)
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
