
using System;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectSongQueueControl : INeedInjection, IInjectionFinishedListener
{
    private const float SongQueueItemHeightInPx = 65;

    [Inject(UxmlName = R.UxmlNames.toggleSongQueueOverlayButton)]
    private Button toggleSongQueueOverlayButton;

    [Inject(UxmlName = R.UxmlNames.closeSongQueueButton)]
    private Button closeSongQueueButton;

    [Inject(UxmlName = R.UxmlNames.hiddenHideSongQueueOverlayArea)]
    private VisualElement hiddenHideSongQueueOverlayArea;

    [Inject(UxmlName = R.UxmlNames.songQueueLengthContainer)]
    private VisualElement songQueueLengthContainer;

    [Inject(UxmlName = R.UxmlNames.songQueueLengthLabel)]
    private Label songQueueLengthLabel;

    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsNewButton)]
    private Button addToSongQueueAsNewButton;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsMedleyButton)]
    private Button addToSongQueueAsMedleyButton;

    [Inject(UxmlName = R.UxmlNames.startSongQueueButton)]
    private Button startSongQueueButton;

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject]
    private Injector injector;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongSelectSceneData sceneData;

    [Inject]
    private SongSelectPlayerListControl songSelectPlayerListControl;

    private readonly SongQueueUiControl songQueueUiControl = new();

    public VisualElementSlideInControl SongQueueSlideInControl { get; private set; }

    public SongMeta SelectedSong => (songRouletteControl.SelectedEntry as SongSelectSongEntry)?.SongMeta;

    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSongQueueControl.OnInjectionFinished");

        injector.Inject(songQueueUiControl);

        songQueueLengthContainer.HideByDisplay();
        songQueueManager.SongQueueChangedEventStream
            .Subscribe(_ => UpdateSongQueue())
            .AddTo(gameObject);
        UpdateSongQueue();

        addToSongQueueAsNewButton.RegisterCallbackButtonTriggered(_ => AddSongToSongQueue(SelectedSong));
        addToSongQueueAsMedleyButton.RegisterCallbackButtonTriggered(_ => AddSongToSongQueueAsMedley(SelectedSong));
        startSongQueueButton.RegisterCallbackButtonTriggered(_ => StartSingSceneWithNextSongQueueEntry());
        songQueueUiControl.OnToggleMedley = songQueueEntryDto => songQueueManager.ToggleMedley(songQueueEntryDto);
        songQueueUiControl.OnDelete = songQueueEntryDto => songQueueManager.RemoveSongQueueEntry(songQueueEntryDto);
        songQueueUiControl.OnItemIndexChanged = OnItemIndexChanged;
        songQueueUiControl.ItemHeight = SongQueueItemHeightInPx;

        SongQueueSlideInControl = new(songQueueOverlay, ESide2D.Right, false);
        SongSelectSlideInControlUtils.InitSlideInControl(SongQueueSlideInControl, toggleSongQueueOverlayButton, closeSongQueueButton, songQueueOverlay, hiddenHideSongQueueOverlayArea);
    }

    private void OnItemIndexChanged(SongQueueUiControl.ItemIndexChangedEvent evt)
    {
        songQueueManager.SetSongQueueEntries(evt.UpdatedItems.ToList());
    }

    private void UpdateSongQueue()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.UpdateSongQueue");

        string newSongQueueLengthAsString = songQueueManager.SongQueueLength.ToString();
        if (songQueueLengthLabel.text != newSongQueueLengthAsString)
        {
            songQueueLengthContainer.SetVisibleByDisplay(songQueueManager.SongQueueLength > 0);
            songQueueLengthLabel.SetTranslatedText(Translation.Of(newSongQueueLengthAsString));
            LeanTween.value(gameObject, Vector3.one * 1.5f, Vector3.one, 1.5f)
                .setEaseOutBounce()
                .setOnUpdate(s => songQueueLengthLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s))));
        }
        songQueueUiControl.SetSongQueueEntryDtos(songQueueManager.GetSongQueueEntries());
        startSongQueueButton.SetEnabled(!songQueueManager.IsSongQueueEmpty);
    }

    public void AddSongToSongQueue(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings(songMeta);
        songQueueManager.AddSongQueueEntry(songQueueEntryDto);
    }


    public void AddSongToSongQueueAsMedley(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        if (songQueueManager.IsSongQueueEmpty)
        {
            // Cannot create medley with previous song when song queue is empty.
            AddSongToSongQueue(songMeta);
            return;
        }

        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings(songMeta);
        if (songQueueEntryDto != null)
        {
            songQueueEntryDto.IsMedleyWithPreviousEntry = true;
            songQueueManager.AddSongQueueEntry(songQueueEntryDto);
        }
    }

    public void StartSingSceneWithNextSongQueueEntry()
    {
        if (songQueueManager.IsSongQueueEmpty)
        {
            return;
        }

        SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(sceneData.partyModeSceneData);
        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }

    private SongQueueEntryDto CreateSongQueueEntryWithCurrentSettings(SongMeta songMeta)
    {
        SongQueueEntryDto songQueueEntryDto = new();
        songQueueEntryDto.SongDto = DtoConverter.ToDto(songMeta);
        songQueueEntryDto.SingScenePlayerDataDto = DtoConverter.ToDto(songSelectPlayerListControl.CreateSingScenePlayerData());
        songQueueEntryDto.GameRoundSettingsDto = new GameRoundSettingsDto()
        {
            ModifierDtos = DtoConverter.ToDto(nonPersistentSettings.GameRoundSettings.modifiers),
        };
        return songQueueEntryDto;
    }
}
