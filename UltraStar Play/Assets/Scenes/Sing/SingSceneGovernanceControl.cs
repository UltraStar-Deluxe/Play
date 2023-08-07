using System;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneGovernanceControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject(UxmlName = R.UxmlNames.governanceOverlay)]
    private VisualElement governanceOverlay;

    [Inject(UxmlName = R.UxmlNames.togglePlaybackButton)]
    private Button togglePlaybackButton;

    [Inject(UxmlName = R.UxmlNames.playIcon)]
    private VisualElement playIcon;

    [Inject(UxmlName = R.UxmlNames.pauseIcon)]
    private VisualElement pauseIcon;

    [Inject(UxmlName = R.UxmlNames.volumeSlider)]
    private SliderInt volumeSlider;

    [Inject(UxmlName = R.UxmlNames.openControlsMenuButton)]
    private Button openControlsMenuButton;

    [Inject(UxmlName = R.UxmlNames.bottomControlsContainer)]
    private VisualElement bottomControlsContainer;

    [Inject(UxmlName = R.UxmlNames.artistLabel)]
    private Label artistLabel;

    [Inject(UxmlName = R.UxmlNames.titleLabel)]
    private Label titleLabel;

    [Inject(UxmlName = R.UxmlNames.governanceOverlayDetailedTimeBar)]
    private VisualElement governanceOverlayDetailedTimeBar;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneWebcamControl webcamControl;

    [Inject]
    private VolumeControl volumeControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private ThemeManager themeManager;

    private ContextMenuControl contextMenuControl;

    private Vector2 lastPointerPosition;
    private float hideDelayInSeconds;
    private readonly float longHideDelayInSeconds = 2f;
    private readonly float shortHideDelayInSeconds = 0.2f;

    private bool isPointerOverBottomControls;
    private bool playbackJustStarted;
    private float playbackStartTimeInSeconds;

    private bool isPopupMenuOpen;
    private float popupMenuClosedTimeInSeconds;

    private bool fillAppearanceContextMenu;

    private float doNotShowOverlayBeforeTimeInSeconds;

    private bool isContextMenuOpenedFromInputAction;

    private InputAction showOverlayInputAction;

    public void OnInjectionFinished()
    {
        showOverlayInputAction = InputManager.GetInputAction(R.InputActions.usplay_singSceneShowGovernanceOverlay).InputAction;

        contextMenuControl = injector
            .WithRootVisualElement(openControlsMenuButton)
            .CreateAndInject<ContextMenuControl>();
        contextMenuControl.FillContextMenuAction = FillContextMenu;
        contextMenuControl.ContextMenuOpenedEventStream.Subscribe(OnContextMenuOpened);
        contextMenuControl.ContextMenuClosedEventStream.Subscribe(OnContextMenuClosed);

        themeManager.GetCurrentTheme().ThemeJson.primaryFontColor.IfNotDefault(color =>
            governanceOverlayDetailedTimeBar.Query(R.UxmlNames.timeBarPositionIndicator)
                .ForEach(it => it.style.backgroundColor = new StyleColor(color)));

        openControlsMenuButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (isPopupMenuOpen
                || !TimeUtils.IsDurationAboveThresholdInSeconds(popupMenuClosedTimeInSeconds, 0.1f))
            {
                return;
            }

            contextMenuControl.OpenContextMenu(Vector2.zero);
        });

        volumeSlider.RegisterValueChangedCallback(evt =>
        {
            if (settings.VolumePercent != evt.newValue)
            {
                settings.VolumePercent = evt.newValue;
            }
        });
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(newValue =>
            {
                if (volumeSlider.value != newValue)
                {
                    volumeSlider.value = newValue;
                }
            });

        togglePlaybackButton.RegisterCallbackButtonTriggered(_ => TogglePlayPause());
        governanceOverlay.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (isPopupMenuOpen
                || !TimeUtils.IsDurationAboveThresholdInSeconds(popupMenuClosedTimeInSeconds, 0.1f))
            {
                return;
            }

            if (evt.button == 0)
            {
                TogglePlayPause();
            }
        });
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(_ =>
        {
            playbackStartTimeInSeconds = Time.time;
            hideDelayInSeconds = shortHideDelayInSeconds;
            UpdatePlaybackIcon();
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ => UpdatePlaybackIcon());
        songAudioPlayer.LoadedEventStream.Subscribe(_ => UpdatePlaybackIcon());
        UpdatePlaybackIcon();

        bottomControlsContainer.RegisterCallback<PointerEnterEvent>(evt => isPointerOverBottomControls = true);
        bottomControlsContainer.RegisterCallback<PointerLeaveEvent>(evt => isPointerOverBottomControls = false);
        bottomControlsContainer.style.backgroundImage = new StyleBackground(GradientManager.GetGradientTexture(new()
        {
            startColor = Colors.black,
            endColor = Colors.clearBlack,
        }));
        bottomControlsContainer.style.backgroundColor = new StyleColor(Colors.clearBlack);

        artistLabel.text = songMeta.Artist;
        titleLabel.text = songMeta.Title;

        // Hide by default, show on mouse move or key press.
        doNotShowOverlayBeforeTimeInSeconds = Time.time + 0.5f;
        lastPointerPosition = Input.mousePosition;
        hideDelayInSeconds = longHideDelayInSeconds;
        HideOverlayAndCursor();
    }

    public void Update()
    {
        Vector2 currentPointerPosition = InputUtils.GetCurrentPointerPosition();
        if (Time.time > doNotShowOverlayBeforeTimeInSeconds)
        {
            UpdateShowOverlayAndCursorByInput(currentPointerPosition);
        }

        lastPointerPosition = currentPointerPosition;
    }

    private void UpdateShowOverlayAndCursorByInput(Vector2 currentPointerPosition)
    {
        if ((lastPointerPosition != currentPointerPosition
            && currentPointerPosition.x >= 0 && currentPointerPosition.x <= Screen.width
            && currentPointerPosition.y >= 0 && currentPointerPosition.y <= Screen.height)
            || showOverlayInputAction.ReadValue<float>() > 0.5f)
        {
            ShowOverlayAndCursor();
            if (Time.time - playbackStartTimeInSeconds < 0.5f)
            {
                hideDelayInSeconds = shortHideDelayInSeconds;
            }
            else
            {
                hideDelayInSeconds = longHideDelayInSeconds;
            }
        }
        else if (hideDelayInSeconds <= 0
                 && songAudioPlayer.IsPlaying
                 && !isPointerOverBottomControls)
        {
            HideOverlayAndCursor();
        }
        else if (songAudioPlayer.IsPlaying
                 && !isPointerOverBottomControls)
        {
            hideDelayInSeconds -= Time.deltaTime;
        }
    }

    private void HideOverlayAndCursor()
    {
        governanceOverlay.style.opacity = 0;
        Cursor.visible = false;
    }

    private void ShowOverlayAndCursor()
    {
        governanceOverlay.style.opacity = 1;
        Cursor.visible = true;
    }

    public void Dispose()
    {
        Cursor.visible = true;
    }

    private void TogglePlayPause()
    {
        singSceneControl.TogglePlayPause();
        UpdatePlaybackIcon();
    }

    private void UpdatePlaybackIcon()
    {
        playIcon.SetVisibleByDisplay(!songAudioPlayer.IsPlaying);
        pauseIcon.SetVisibleByDisplay(songAudioPlayer.IsPlaying);
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        if (fillAppearanceContextMenu)
        {
            fillAppearanceContextMenu = false;
            FillAppearanceContextMenu(contextMenuPopup);
        }
        else
        {
            FillRegularContextMenu(contextMenuPopup);
        }
    }

    private void FillAppearanceContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        ItemPicker noteDisplayModePicker = new("Note Display Mode¹");
        contextMenuPopup.AddVisualElement(noteDisplayModePicker);
        new NoteDisplayModeItemPickerControl(noteDisplayModePicker)
            .Bind(() => settings.NoteDisplayMode,
                 newValue => settings.NoteDisplayMode = newValue);

        ItemPicker showSongProgressBarPicker = new("Progress Bar");
        contextMenuPopup.AddVisualElement(showSongProgressBarPicker);
        new LabeledItemPickerControl<ESongProgressBar>(showSongProgressBarPicker, EnumUtils.GetValuesAsList<ESongProgressBar>())
            .Bind(() => settings.ShowSongProgressBar,
                newValue => settings.ShowSongProgressBar = newValue);

        Toggle showLyricsOnNotesToggle = new("Lyrics on Notes¹");
        contextMenuPopup.AddVisualElement(showLyricsOnNotesToggle);
        FieldBindingUtils.Bind(showLyricsOnNotesToggle,
            () => settings.ShowLyricsOnNotes,
            newValue => settings.ShowLyricsOnNotes = newValue);

        Toggle showStaticLyricsToggle = new("Lyrics Box¹");
        contextMenuPopup.AddVisualElement(showStaticLyricsToggle);
        FieldBindingUtils.Bind(showStaticLyricsToggle,
            () => settings.ShowStaticLyrics,
            newValue => settings.ShowStaticLyrics = newValue);

        Toggle wipeLyricsToggle = new("Wipe lyrics");
        contextMenuPopup.AddVisualElement(wipeLyricsToggle);
        FieldBindingUtils.Bind(wipeLyricsToggle,
            () => settings.WipeLyrics,
            newValue => settings.WipeLyrics = newValue);

        Toggle showPitchIndicatorToggle = new("Pitch Arrow");
        contextMenuPopup.AddVisualElement(showPitchIndicatorToggle);
        FieldBindingUtils.Bind(showPitchIndicatorToggle,
            () => settings.ShowPitchIndicator,
                newValue => settings.ShowPitchIndicator = newValue);

        Toggle showPlayerNamesToggle = new("Player Name");
        contextMenuPopup.AddVisualElement(showPlayerNamesToggle);
        FieldBindingUtils.Bind(showPlayerNamesToggle,
            () => settings.ShowPlayerNames,
            newValue => settings.ShowPlayerNames = newValue);

        Toggle showScoreNumbers = new("Player Score");
        contextMenuPopup.AddVisualElement(showScoreNumbers);
        FieldBindingUtils.Bind(showScoreNumbers,
            () => settings.ShowScoreNumbers,
            newValue => settings.ShowScoreNumbers = newValue);

        Toggle showPlayerInfoNextToNotesToggle = new("Player alongside notes¹");
        contextMenuPopup.AddVisualElement(showPlayerInfoNextToNotesToggle);
        FieldBindingUtils.Bind(showPlayerInfoNextToNotesToggle,
            () => settings.ShowPlayerInfoNextToNotes,
            newValue => settings.ShowPlayerInfoNextToNotes = newValue);

        if (webcamControl.WebcamsAvailable())
        {
            Toggle webcamToggle = new("Webcam");
            contextMenuPopup.AddVisualElement(webcamToggle);
            FieldBindingUtils.Bind(webcamToggle,
                () => settings.UseWebcamAsBackgroundInSingScene,
                newValue => webcamControl.SetUseAsBackgroundInSingScene(newValue));
        }

        contextMenuPopup.AddSeparator();
        contextMenuPopup.AddVisualElement(new Label("¹ Requires restart"));
        contextMenuPopup.AddButton("Restart Now", "replay",
            () => singSceneControl.Restart());
    }

    private void FillRegularContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics), "skip_next",
            () => singSceneControl.SkipToNextSingableNoteOrEndOfSong());
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_restart), "replay",
            () => singSceneControl.Restart());

        contextMenuPopup.AddButton("Appearance", "filter_b_and_w", () =>
        {
            bool wasContextMenuOpenedFromInputAction = isContextMenuOpenedFromInputAction;
            fillAppearanceContextMenu = true;
            contextMenuPopup.CloseContextMenu();
            isContextMenuOpenedFromInputAction = wasContextMenuOpenedFromInputAction;
            contextMenuControl.OpenContextMenu(Vector2.zero);
        });

        contextMenuPopup.AddButton("Attribution", "info_outline", () =>
        {
            singSceneControl.Pause();
            ShowSongInfoDialog();
        });

        if (!singSceneControl.HasPartyModeSceneData)
        {
            contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_openSongEditor), "edit",
                () => singSceneControl.OpenSongInEditor());
        }

        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_exitSong), "logout",
            () => singSceneControl.FinishScene(false, false));

        contextMenuPopup.AddSeparator();

        // Button to separate audio or slider to change vocals audio
        if (SongMetaUtils.VocalsAudioResourceExists(singSceneControl.SongMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(singSceneControl.SongMeta))
        {
            contextMenuPopup.AddVisualElement(new Label("Vocals Volume"));
            Slider vocalsVolumeSlider = new();
            vocalsVolumeSlider.lowValue = 0;
            vocalsVolumeSlider.highValue = 100;
            vocalsVolumeSlider.value = settings.VocalsAudioVolumePercent;
            vocalsVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                settings.VocalsAudioVolumePercent = (int)evt.newValue;
            });

            contextMenuPopup.AddVisualElement(vocalsVolumeSlider);
        }
        else
        {
            contextMenuPopup.AddButton("Separate audio", "call_split",
                () => audioSeparationManager.ProcessSongMeta(singSceneControl.SongMeta));
        }
    }

    private void ShowSongInfoDialog()
    {
        MessageDialogControl messageDialogControl = UiManager.Instance.CreateDialogControl("Attribution");
        messageDialogControl.AddVisualElement(AttributionUtils.CreateAttributionVisualElement(songMeta));
        messageDialogControl.AddButton("Close", _ => messageDialogControl.CloseDialog());
    }

    private void OnContextMenuClosed(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = false;
        isContextMenuOpenedFromInputAction = false;
        popupMenuClosedTimeInSeconds = Time.time;
    }

    private void OnContextMenuOpened(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = true;
        new AnchoredPopupControl(contextMenuPopupControl.VisualElement, openControlsMenuButton, Corner2D.TopRight);
        contextMenuPopupControl.VisualElement.AddToClassList("singSceneContextMenu");

        if (isContextMenuOpenedFromInputAction)
        {
            FocusFirstButton(contextMenuPopupControl);
        }
    }

    private void FocusFirstButton(ContextMenuPopupControl contextMenuPopupControl)
    {
        contextMenuPopupControl.VisualElement.Query<Button>().ToList().FirstOrDefault().Focus();
    }

    public void OpenContextMenuFromInputAction()
    {
        isContextMenuOpenedFromInputAction = true;
        contextMenuControl.OpenContextMenu(Vector2.zero);
    }
}
