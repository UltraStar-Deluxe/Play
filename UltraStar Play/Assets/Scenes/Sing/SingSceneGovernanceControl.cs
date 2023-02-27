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
    
    [Inject(UxmlName = R.UxmlNames.toggleMuteButton)]
    private Button toggleMuteButton;
    
    [Inject(UxmlName = R.UxmlNames.muteIcon)]
    private VisualElement muteIcon;
    
    [Inject(UxmlName = R.UxmlNames.unmuteIcon)]
    private VisualElement unmuteIcon;
    
    [Inject(UxmlName = R.UxmlNames.openControlsMenuButton)]
    private Button openControlsMenuButton;
    
    [Inject(UxmlName = R.UxmlNames.bottomControls)]
    private VisualElement bottomControls;
    
    [Inject(UxmlName = R.UxmlNames.artistLabel)]
    private Label artistLabel;
    
    [Inject(UxmlName = R.UxmlNames.titleLabel)]
    private Label titleLabel;
    
    [Inject]
    private Injector injector;
    
    [Inject]
    private SongMeta songMeta;
    
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
    
    public void OnInjectionFinished()
    {
        contextMenuControl = injector
            .WithRootVisualElement(openControlsMenuButton)
            .CreateAndInject<ContextMenuControl>();
        contextMenuControl.FillContextMenuAction = FillContextMenu;
        contextMenuControl.ContextMenuOpenedEventStream.Subscribe(OnContextMenuOpened);
        contextMenuControl.ContextMenuClosedEventStream.Subscribe(OnContextMenuClosed);
        
        openControlsMenuButton.RegisterCallbackButtonTriggered(() =>
        {
            if (isPopupMenuOpen
                || !TimeUtils.IsDurationAboveThreshold(popupMenuClosedTimeInSeconds, 0.1f))
            {
                return;
            }

            contextMenuControl.OpenContextMenu(Vector2.zero);
        });
        
        toggleMuteButton.RegisterCallbackButtonTriggered(() =>
        {
            ToggleMute();
        });
        UpdateMuteIcon();
        
        togglePlaybackButton.RegisterCallbackButtonTriggered(() => TogglePlayPause());
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

        bottomControls.RegisterCallback<PointerEnterEvent>(evt => isPointerOverBottomControls = true);
        bottomControls.RegisterCallback<PointerLeaveEvent>(evt => isPointerOverBottomControls = false);

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

    private void ToggleMute()
    {
        volumeControl.ToggleMuteAudio();
        UpdateMuteIcon();
    }

    private void UpdateMuteIcon()
    {
        muteIcon.SetVisibleByDisplay(volumeControl.IsMuted);
        unmuteIcon.SetVisibleByDisplay(!volumeControl.IsMuted);
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        webcamControl.AddToContextMenu(contextMenuPopup);
        
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_restart),
            () => singSceneControl.Restart());
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
            () => singSceneControl.SkipToNextSingableNote());
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_exitSong),
            () => singSceneControl.FinishScene(false));
        contextMenuPopup.AddItem(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => singSceneControl.OpenSongInEditor());
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
