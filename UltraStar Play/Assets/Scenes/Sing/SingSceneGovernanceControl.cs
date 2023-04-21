using System;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
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
    
    private ContextMenuControl contextMenuControl;

    private Vector3 lastMousePosition;
    private float hideDelayInSeconds;
    private readonly float longHideDelayInSeconds = 2f;
    private readonly float shortHideDelayInSeconds = 0.2f;

    private bool isPointerOverBottomControls;
    private bool playbackJustStarted;
    private float playbackStartTimeInSeconds;
    
    private bool isPopupMenuOpen;
    private float popupMenuClosedTimeInSeconds;

    private bool fillAppearanceContextMenu;
    
    public void OnInjectionFinished()
    {
        contextMenuControl = injector
            .WithRootVisualElement(openControlsMenuButton)
            .CreateAndInject<ContextMenuControl>();
        contextMenuControl.FillContextMenuAction = FillContextMenu;
        contextMenuControl.ContextMenuOpenedEventStream.Subscribe(OnContextMenuOpened);
        contextMenuControl.ContextMenuClosedEventStream.Subscribe(OnContextMenuClosed);
        
        openControlsMenuButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (isPopupMenuOpen
                || !TimeUtils.IsDurationAboveThreshold(popupMenuClosedTimeInSeconds, 0.1f))
            {
                return;
            }

            contextMenuControl.OpenContextMenu(Vector2.zero);
        });
        
        volumeSlider.RegisterValueChangedCallback(evt =>
        {
            if (settings.AudioSettings.VolumePercent != evt.newValue)
            {
                settings.AudioSettings.VolumePercent = evt.newValue;
            }
        });
        settings.ObserveEveryValueChanged(it => it.AudioSettings.VolumePercent)
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
                || !TimeUtils.IsDurationAboveThreshold(popupMenuClosedTimeInSeconds, 0.1f))
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
        lastMousePosition = Input.mousePosition;
        hideDelayInSeconds = longHideDelayInSeconds;
        HideOverlayAndCursor();
    }

    public void Update()
    {
        if (lastMousePosition != Input.mousePosition
            || Input.anyKeyDown)
        {
            lastMousePosition = Input.mousePosition;

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

        if (hideDelayInSeconds <= 0
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
            .Bind(() => settings.GraphicSettings.noteDisplayMode,
                 newValue => settings.GraphicSettings.noteDisplayMode = newValue);
        
        Toggle showLyricsOnNotesToggle = new("Lyrics on Notes¹");
        contextMenuPopup.AddVisualElement(showLyricsOnNotesToggle);
        FieldBindingUtils.Bind(showLyricsOnNotesToggle,
            () => settings.GraphicSettings.showLyricsOnNotes,
            newValue => settings.GraphicSettings.showLyricsOnNotes = newValue);
        
        Toggle showStaticLyricsToggle = new("Lyrics Box¹");
        contextMenuPopup.AddVisualElement(showStaticLyricsToggle);
        FieldBindingUtils.Bind(showStaticLyricsToggle,
            () => settings.GraphicSettings.showStaticLyrics,
            newValue => settings.GraphicSettings.showStaticLyrics = newValue);
        
        Toggle showPitchIndicatorToggle = new("Pitch Arrow");
        contextMenuPopup.AddVisualElement(showPitchIndicatorToggle);
        FieldBindingUtils.Bind(showPitchIndicatorToggle,
            () => settings.GraphicSettings.showPitchIndicator,
                newValue => settings.GraphicSettings.showPitchIndicator = newValue);
        
        Toggle showPlayerNamesToggle = new("Player Names");
        contextMenuPopup.AddVisualElement(showPlayerNamesToggle);
        FieldBindingUtils.Bind(showPlayerNamesToggle,
            () => settings.GraphicSettings.showPlayerNames,
            newValue => settings.GraphicSettings.showPlayerNames = newValue);
        
        Toggle showScoreNumbers = new("Player Score");
        contextMenuPopup.AddVisualElement(showScoreNumbers);
        FieldBindingUtils.Bind(showScoreNumbers,
            () => settings.GraphicSettings.showScoreNumbers,
            newValue => settings.GraphicSettings.showScoreNumbers = newValue);
        
        if (webcamControl.WebcamsAvailable())
        {
            Toggle webcamToggle = new("Webcam");
            contextMenuPopup.AddVisualElement(webcamToggle);
            FieldBindingUtils.Bind(webcamToggle,
                () => settings.WebcamSettings.UseAsBackgroundInSingScene,
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
            fillAppearanceContextMenu = true;
            contextMenuPopup.CloseContextMenu();
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
            vocalsVolumeSlider.value = settings.AudioSettings.VocalsAudioVolumePercent;
            vocalsVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                settings.AudioSettings.VocalsAudioVolumePercent = (int)evt.newValue;
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
        popupMenuClosedTimeInSeconds = Time.time;
    }
    
    private void OnContextMenuOpened(ContextMenuPopupControl contextMenuPopupControl)
    {
        isPopupMenuOpen = true;
        new AnchoredPopupControl(contextMenuPopupControl.VisualElement, openControlsMenuButton, Corner2D.TopRight);
        contextMenuPopupControl.VisualElement.AddToClassList("singSceneContextMenu");
    }
}
