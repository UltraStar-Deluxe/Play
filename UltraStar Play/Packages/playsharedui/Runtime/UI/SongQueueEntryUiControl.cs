using System;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueEntryUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(Key = nameof(songQueuePlayerEntryUi))]
    private VisualTreeAsset songQueuePlayerEntryUi;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songArtist)]
    private Label songArtist;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songTitle)]
    private Label songTitle;

    [Inject(UxmlName = R_PlayShared.UxmlNames.songQueueEntryModifierActiveIcon)]
    private VisualElement songQueueEntryModifierActiveIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.playerEntryList)]
    private VisualElement playerEntryList;

    [Inject(UxmlName = R_PlayShared.UxmlNames.isMedleyIcon)]
    private VisualElement isMedleyIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.isNoMedleyIcon)]
    private VisualElement isNoMedleyIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.toggleMedleyButton)]
    private Button toggleMedleyButton;
    public Button ToggleMedleyButton => toggleMedleyButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.deleteButton)]
    private Button deleteButton;
    public Button DeleteButton => deleteButton;

    [Inject]
    public SongQueueEntryDto SongQueueEntryDto { get; private set; }

    public Action OnDelete { get; set; }
    public Action OnToggleMedley { get; set; }

    public void OnInjectionFinished()
    {
        deleteButton.RegisterCallbackButtonTriggered(_ => OnDelete?.Invoke());
        toggleMedleyButton.RegisterCallbackButtonTriggered(_ => OnToggleMedley?.Invoke());

        // Is medley or not
        isMedleyIcon.SetVisibleByDisplay(SongQueueEntryDto.IsMedleyWithPreviousEntry);
        isNoMedleyIcon.SetVisibleByDisplay(!SongQueueEntryDto.IsMedleyWithPreviousEntry);

        // Add song entries
        songArtist.text = SongQueueEntryDto.SongDto.Artist;
        songTitle.text = SongQueueEntryDto.SongDto.Title;

        // Any modifier active
        songQueueEntryModifierActiveIcon.SetVisibleByDisplay(
            !SongQueueEntryDto.IsMedleyWithPreviousEntry
            && SongQueueEntryDto.GameRoundSettingsDto.AnyModifierActive);

        // Add player entries
        playerEntryList.Clear();
        SongQueueEntryDto.SingScenePlayerDataDto.PlayerProfileNames.ForEach(playerProfileName =>
        {
            VisualElement playerEntry = songQueuePlayerEntryUi.CloneTreeAndGetFirstChild();
            playerEntryList.Add(playerEntry);
            playerEntry.Q<Label>(R_PlayShared.UxmlNames.nameLabel).text = playerProfileName;
            VisualElement micIcon = playerEntry.Q<VisualElement>(R_PlayShared.UxmlNames.micIcon);
            if (SongQueueEntryDto.SingScenePlayerDataDto.PlayerProfileToMicProfileMap.TryGetValue(playerProfileName, out MicProfileDto micProfileDto))
            {
                micIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfileDto.Color);
                micIcon.style.color = new StyleColor(micProfileDto.Color);
            }
            else
            {
                micIcon.HideByDisplay();
            }
        });
    }

    public void HideToggleMedleyButton()
    {
        toggleMedleyButton.HideByDisplay();
    }

    public void HideControls()
    {
        deleteButton.HideByDisplay();
        toggleMedleyButton.HideByDisplay();
    }
}
