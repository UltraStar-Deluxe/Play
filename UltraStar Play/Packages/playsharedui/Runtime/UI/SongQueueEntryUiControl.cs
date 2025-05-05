using System;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueEntryUiControl : INeedInjection
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

    [Inject(UxmlName = R_PlayShared.UxmlNames.deleteButton)]
    private Button deleteButton;

    private SongQueueEntryDto songQueueEntryDto;
    public SongQueueEntryDto SongQueueEntryDto
    {
        get => songQueueEntryDto;
        set
        {
            songQueueEntryDto = value;
            UpdateSongQueueEntryUi();

            if (songQueueEntryDto == null)
            {
                UnregisterCallback();
            }
            else
            {
                RegisterCallback();
            }
        }
    }

    public Action OnDelete { get; set; }
    public Action OnToggleMedley { get; set; }

    private void RegisterCallback()
    {
        deleteButton.RegisterCallbackButtonTriggered(OnDeleteCallback);
        toggleMedleyButton.RegisterCallbackButtonTriggered(OnToggleMedleyCallback);
    }

    private void UnregisterCallback()
    {
        deleteButton.UnregisterCallbackButtonTriggered(OnDeleteCallback);
        toggleMedleyButton.UnregisterCallbackButtonTriggered(OnToggleMedleyCallback);
    }

    public void ShowToggleMedleyButton()
    {
        toggleMedleyButton.ShowByDisplay();
    }

    public void HideToggleMedleyButton()
    {
        toggleMedleyButton.HideByDisplay();
    }

    public void HideControls()
    {
        deleteButton.HideByDisplay();
        HideToggleMedleyButton();
    }

    private void UpdateSongQueueEntryUi()
    {
        if (SongQueueEntryDto == null)
        {
            songArtist.text = "";
            songTitle.text = "";
            isMedleyIcon.HideByDisplay();
            isNoMedleyIcon.HideByDisplay();
            songQueueEntryModifierActiveIcon.HideByDisplay();
            playerEntryList.Clear();
            return;
        }

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

    private void OnToggleMedleyCallback(EventBase evt)
    {
        OnToggleMedley?.Invoke();
    }

    private void OnDeleteCallback(EventBase evt)
    {
        OnDelete?.Invoke();
    }
}
