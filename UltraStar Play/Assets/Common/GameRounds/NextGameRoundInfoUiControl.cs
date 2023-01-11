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

public class NextGameRoundUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(nextGameRoundInfoPlayerEntryUi))]
    private VisualTreeAsset nextGameRoundInfoPlayerEntryUi;

    [Inject(UxmlName = R.UxmlNames.nextGameRoundInfoUiRoot)]
    private VisualElement nextGameRoundInfoUiRoot;

    [Inject(UxmlName = R.UxmlNames.nextGameRoundSongInfoLabel)]
    private Label nextGameRoundSongInfoLabel;

    [Inject(UxmlName = R.UxmlNames.nextGameRoundPlayerEntryList)]
    private VisualElement nextGameRoundPlayerEntryList;

    [Inject]
    private GameRoundManager gameRoundManager;

    public void OnInjectionFinished()
    {
        if (!gameRoundManager.HasGameRounds)
        {
            HideNextGameRoundUi();
            return;
        }

        GameRoundData gameRound = gameRoundManager.GetNextGameRound();
        nextGameRoundSongInfoLabel.text = SongMetaUtils.GetMedleyName(gameRound.SongMetas);

        nextGameRoundPlayerEntryList.RemoveTemplateContainers();
        gameRound.SingScenePlayerData.SelectedPlayerProfiles.ForEach(playerProfile =>
        {
            VisualElement playerEntryVisualElement = nextGameRoundInfoPlayerEntryUi.CloneTree().Children().FirstOrDefault();
            nextGameRoundPlayerEntryList.Add(playerEntryVisualElement);
            playerEntryVisualElement.Q<Label>().text = playerProfile.Name;
            VisualElement micVisualElement = playerEntryVisualElement.Q<VisualElement>(R.UxmlNames.nextGameRoundPlayerEntryMicImage);
            if (gameRound.SingScenePlayerData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile))
            {
                micVisualElement.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            }
            else
            {
                micVisualElement.HideByDisplay();
            }
        });
    }

    public void HideNextGameRoundUi()
    {
        nextGameRoundInfoUiRoot.HideByDisplay();
    }
}
