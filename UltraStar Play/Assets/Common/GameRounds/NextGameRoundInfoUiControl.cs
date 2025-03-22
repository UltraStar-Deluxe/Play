using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine.UIElements;

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

    [Inject(UxmlName = R.UxmlNames.nextGameRoundModifierActiveIcon)]
    private VisualElement nextGameRoundModifierActiveIcon;

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject]
    private SongMetaManager songMetaManager;

    public void OnInjectionFinished()
    {
        if (songQueueManager.IsSongQueueEmpty)
        {
            HideNextGameRoundUi();
            return;
        }

        List<SongQueueEntryDto> songQueueEntryDtos = songQueueManager.PeekNextSongQueueEntries();
        List<SongMeta> songMetas = songQueueEntryDtos
            .Select(it => DtoConverter.FromDto(it.SongDto, songMetaManager))
            .ToList();
        nextGameRoundSongInfoLabel.SetTranslatedText(Translation.Get(R.Messages.songQueue_nextEntry,
            "value", GetMedleyName(songMetas)));

        nextGameRoundPlayerEntryList.RemoveTemplateContainers();
        SongQueueEntryDto songQueueEntryDto = songQueueEntryDtos.FirstOrDefault();
        songQueueEntryDto.SingScenePlayerDataDto.PlayerProfileNames.ForEach(playerProfileName =>
        {
            VisualElement playerEntryVisualElement = nextGameRoundInfoPlayerEntryUi.CloneTree().Children().FirstOrDefault();
            nextGameRoundPlayerEntryList.Add(playerEntryVisualElement);

            Label playerNameLabel = playerEntryVisualElement.Q<Label>(R.UxmlNames.nextGameRoundPlayerEntryLabel);
            playerNameLabel.SetTranslatedText(Translation.Of(playerProfileName));

            VisualElement micVisualElement = playerEntryVisualElement.Q<VisualElement>(R.UxmlNames.nextGameRoundPlayerEntryMicImage);
            if (songQueueEntryDto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap.TryGetValue(playerProfileName, out MicProfileDto micProfileDto))
            {
                micVisualElement.ShowByDisplay();
                micVisualElement.style.color = new StyleColor(micProfileDto.Color);
                micVisualElement.style.unityBackgroundImageTintColor = new StyleColor(micProfileDto.Color);
            }
            else
            {
                micVisualElement.HideByDisplay();
            }
        });

        nextGameRoundModifierActiveIcon.SetVisibleByDisplay(songQueueEntryDto.GameRoundSettingsDto.AnyModifierActive);
    }

    public void HideNextGameRoundUi()
    {
        nextGameRoundInfoUiRoot.HideByDisplay();
    }

    private static string GetMedleyName(List<SongMeta> songMetas)
    {
        if (songMetas.IsNullOrEmpty())
        {
            return "";
        }

        if (songMetas.Count == 1)
        {
            songMetas[0].GetArtistDashTitle();
        }

        return songMetas
            .Select(songMeta => songMeta.Title)
            .JoinWith(", ");
    }
}
