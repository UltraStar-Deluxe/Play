using System;
using System.Linq;
using PrimeInputActions;
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

            contextMenuControl.OpenContextMenu(Vector2.zero, this);
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
            UpdatePlaybackIcon(true);
        });
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(_ => UpdatePlaybackIcon(false));
        songAudioPlayer.LoadedEventStream.Subscribe(_ => UpdatePlaybackIcon(true));
        UpdatePlaybackIcon(false);

        bottomControlsContainer.RegisterCallback<PointerEnterEvent>(evt => isPointerOverBottomControls = true);
        bottomControlsContainer.RegisterCallback<PointerLeaveEvent>(evt => isPointerOverBottomControls = false);
        bottomControlsContainer.style.backgroundImage = new StyleBackground(GradientManager.GetGradientTexture(new()
        {
            startColor = Colors.black,
            endColor = Colors.clearBlack,
        }));
        bottomControlsContainer.style.backgroundColor = new StyleColor(Colors.clearBlack);

        artistLabel.SetTranslatedText(Translation.Of(songMeta.Artist));
        titleLabel.SetTranslatedText(Translation.Of(songMeta.Title));

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
    }

    private void UpdatePlaybackIcon(bool isPlaying)
    {
        playIcon.SetVisibleByDisplay(!isPlaying);
        pauseIcon.SetVisibleByDisplay(isPlaying);
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
        Translation TranslationThatRequiresRestart(string translationKey)
        {
            return Translation.Of(Translation.Get(translationKey) + "¹");
        }

        Chooser noteDisplayModeChooser = new();
        noteDisplayModeChooser.SetTranslatedLabel(TranslationThatRequiresRestart(R.Messages.singScene_options_noteDisplayMode));
        contextMenuPopup.AddVisualElement(noteDisplayModeChooser);
        new EnumChooserControl<ENoteDisplayMode>(noteDisplayModeChooser)
            .Bind(() => settings.NoteDisplayMode,
                 newValue => settings.NoteDisplayMode = newValue);

        Chooser lineCountChooser = new();
        lineCountChooser.SetTranslatedLabel(TranslationThatRequiresRestart(R.Messages.singScene_options_noteDisplayLineCount));
        contextMenuPopup.AddVisualElement(lineCountChooser);
        new EnumChooserControl<ENoteDisplayLineCount>(lineCountChooser)
            .Bind(() => ToLineCountEnum(settings.NoteDisplayLineCount),
                newValue => settings.NoteDisplayLineCount = ToLineCount(newValue));

        Chooser showSongProgressBarChooser = new();
        showSongProgressBarChooser.SetTranslatedLabel(Translation.Get(R.Messages.singScene_options_showProgressBar));
        contextMenuPopup.AddVisualElement(showSongProgressBarChooser);
        new EnumChooserControl<ESongProgressBar>(showSongProgressBarChooser)
            .Bind(() => settings.ShowSongProgressBar,
                newValue => settings.ShowSongProgressBar = newValue);

        Chooser staticLyricsDisplayModeChooser = new();
        staticLyricsDisplayModeChooser.SetTranslatedLabel(TranslationThatRequiresRestart(R.Messages.singScene_options_showLyricsArea));
        contextMenuPopup.AddVisualElement(staticLyricsDisplayModeChooser);
        new EnumChooserControl<EStaticLyricsDisplayMode>(staticLyricsDisplayModeChooser)
            .Bind(() => settings.StaticLyricsDisplayMode,
                newValue => settings.StaticLyricsDisplayMode = newValue);

        Toggle showLyricsOnNotesToggle = new();
        showLyricsOnNotesToggle.SetTranslatedLabel(TranslationThatRequiresRestart(R.Messages.singScene_options_showLyricsOnNotes));
        contextMenuPopup.AddVisualElement(showLyricsOnNotesToggle);
        FieldBindingUtils.Bind(showLyricsOnNotesToggle,
            () => settings.ShowLyricsOnNotes,
            newValue => settings.ShowLyricsOnNotes = newValue);

        Toggle showPitchIndicatorToggle = new();
        showPitchIndicatorToggle.SetTranslatedLabel(Translation.Get(R.Messages.singScene_options_showPitchArrow));
        contextMenuPopup.AddVisualElement(showPitchIndicatorToggle);
        FieldBindingUtils.Bind(showPitchIndicatorToggle,
            () => settings.ShowPitchIndicator,
                newValue => settings.ShowPitchIndicator = newValue);

        Toggle showPlayerNamesToggle = new();
        showPlayerNamesToggle.SetTranslatedLabel(Translation.Get(R.Messages.singScene_options_showPlayerName));
        contextMenuPopup.AddVisualElement(showPlayerNamesToggle);
        FieldBindingUtils.Bind(showPlayerNamesToggle,
            () => settings.ShowPlayerNames,
            newValue => settings.ShowPlayerNames = newValue);

        Toggle showScoreNumbers = new();
        showScoreNumbers.SetTranslatedLabel(Translation.Get(R.Messages.singScene_options_showPlayerScore));
        contextMenuPopup.AddVisualElement(showScoreNumbers);
        FieldBindingUtils.Bind(showScoreNumbers,
            () => settings.ShowScoreNumbers,
            newValue => settings.ShowScoreNumbers = newValue);

        Toggle showPlayerInfoNextToNotesToggle = new();
        showPlayerInfoNextToNotesToggle.SetTranslatedLabel(TranslationThatRequiresRestart(R.Messages.singScene_options_showPlayerAlongsideNotes));
        contextMenuPopup.AddVisualElement(showPlayerInfoNextToNotesToggle);
        FieldBindingUtils.Bind(showPlayerInfoNextToNotesToggle,
            () => settings.ShowPlayerInfoNextToNotes,
            newValue => settings.ShowPlayerInfoNextToNotes = newValue);

        if (webcamControl.WebcamsAvailable())
        {
            Toggle webcamToggle = new();
            webcamToggle.SetTranslatedLabel(Translation.Get(R.Messages.singScene_options_useWebcamAsBackground));
            contextMenuPopup.AddVisualElement(webcamToggle);
            FieldBindingUtils.Bind(webcamToggle,
                () => settings.UseWebcamAsBackgroundInSingScene,
                newValue => webcamControl.SetUseAsBackgroundInSingScene(newValue));
        }

        contextMenuPopup.AddSeparator();
        contextMenuPopup.AddVisualElement(new Label(Translation.Get(R.Messages.singScene_action_requiresRestart)));
        contextMenuPopup.AddButton(Translation.Get(R.Messages.singScene_action_restart), "replay",
            () => singSceneControl.Restart());
    }

    private int ToLineCount(ENoteDisplayLineCount lineCount)
    {
        switch (lineCount)
        {
            case ENoteDisplayLineCount.Auto: return 0;
            case ENoteDisplayLineCount.Few: return 7;
            case ENoteDisplayLineCount.Medium: return 10;
            case ENoteDisplayLineCount.Many: return 18;
            default: return 10;
        }
    }

    private ENoteDisplayLineCount ToLineCountEnum(int lineCount)
    {
        if (lineCount <= 0)
        {
            return ENoteDisplayLineCount.Auto;
        }
        else if (lineCount < 10)
        {
            return ENoteDisplayLineCount.Few;
        }
        else if (lineCount == 10)
        {
            return ENoteDisplayLineCount.Medium;
        }
        else
        {
            return ENoteDisplayLineCount.Many;
        }
    }

    private void FillRegularContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        contextMenuPopup.AddButton(Translation.Get(R.Messages.action_skipToNextLyrics), "skip_next",
            () => singSceneControl.SkipToNextSingableNoteOrEndOfSong());
        contextMenuPopup.AddButton(Translation.Get(R.Messages.action_restart), "replay",
            () => singSceneControl.Restart());

        contextMenuPopup.AddButton(Translation.Get(R.Messages.singScene_action_openAppearanceSubmenu), "filter_b_and_w", () =>
        {
            bool wasContextMenuOpenedFromInputAction = isContextMenuOpenedFromInputAction;
            fillAppearanceContextMenu = true;
            contextMenuPopup.CloseContextMenu();
            isContextMenuOpenedFromInputAction = wasContextMenuOpenedFromInputAction;
            contextMenuControl.OpenContextMenu(Vector2.zero, this);
        });

        contextMenuPopup.AddButton(Translation.Get(R.Messages.singScene_action_openAttributionSubmenu), "info_outline", () =>
        {
            singSceneControl.Pause();
            ShowSongInfoDialog();
        });

        if (!singSceneControl.HasPartyModeSceneData)
        {
            contextMenuPopup.AddButton(Translation.Get(R.Messages.action_openSongEditor), "edit",
                () => singSceneControl.OpenSongInEditor());
        }

        contextMenuPopup.AddButton(Translation.Get(R.Messages.action_exitSong), "logout",
            () => singSceneControl.FinishScene(false, false));

        contextMenuPopup.AddSeparator();

        // Button to separate audio or slider to change vocals audio
        if (SongMetaUtils.VocalsAudioResourceExists(singSceneControl.SongMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(singSceneControl.SongMeta))
        {
            contextMenuPopup.AddVisualElement(new Label(Translation.Get(R.Messages.singScene_options_vocalsVolume)));
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
            contextMenuPopup.AddButton(Translation.Get(R.Messages.action_separateAudio), "call_split",
                () => audioSeparationManager.ProcessSongMeta(singSceneControl.SongMeta, true));
        }
    }

    private void ShowSongInfoDialog()
    {
        MessageDialogControl messageDialogControl = UiManager.Instance.CreateDialogControl(Translation.Get(R.Messages.action_showAttribution));
        messageDialogControl.AddVisualElement(AttributionUtils.CreateAttributionVisualElement(songMeta));
        messageDialogControl.AddButton(Translation.Get(R.Messages.action_close), _ => messageDialogControl.CloseDialog());
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
        contextMenuControl.OpenContextMenu(Vector2.zero, this);
    }
}
