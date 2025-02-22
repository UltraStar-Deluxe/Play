using System;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSongPreviewControl : SongPreviewControl
{
    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneData sceneData;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.songPreviewVideoImage)]
    private VisualElement songPreviewVideoImage;

    [Inject(UxmlName = R.UxmlNames.songPreviewBackgroundImage)]
    private VisualElement songPreviewBackgroundImage;

    private SongSelectEntryControl currentSongSelectEntryControl;

    private int initialSongIndex;

    // The very first selected song should not be previewed.
    // The song is selected when opening the scene.
    private bool isFirstSelectedSong = true;

    protected override void Start()
    {
        base.Start();

        AudioFadeInDurationInSeconds = settings.PreviewFadeInDurationInSeconds;
        VideoFadeInDurationInSeconds = settings.PreviewFadeInDurationInSeconds;

        songPreviewVideoImage.style.opacity = 0;
        StartSongPreviewEventStream.Subscribe(songMeta =>
        {
            if (currentSongSelectEntryControl == null)
            {
                return;
            }

            string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
            if (SongMetaUtils.ResourceExists(songMeta, videoUri))
            {
                songPreviewVideoImage.ShowByDisplay();
                songPreviewVideoImage.style.opacity = 0;
            }

            songPreviewBackgroundImage.ShowByDisplay();
            songPreviewBackgroundImage.SetBackgroundImageAlpha(0);
        });
        StopSongPreviewEventStream.Subscribe(_ =>
        {
            songPreviewVideoImage.HideByDisplay();
            songPreviewBackgroundImage.HideByDisplay();
        });

        // Video / background image fade-in
        float videoTargetAlpha = themeManager.GetCurrentTheme().ThemeJson.videoPreviewColor.a / 255f;
        if (videoTargetAlpha <= 0)
        {
            videoTargetAlpha = 1;
        }
        VideoFadeIn.Subscribe(newValue =>
        {
            songPreviewVideoImage.style.opacity = newValue * videoTargetAlpha;
        });
        BackgroundImageFadeIn.Subscribe(newValue =>
        {
            songPreviewBackgroundImage.SetBackgroundImageAlpha(newValue);
        });

        if (sceneData != null
            && sceneData.SongMeta != null)
        {
            initialSongIndex = songRouletteControl.GetEntryIndexBySongMeta(sceneData.SongMeta);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (songRouletteControl.Selection.Value.Entry is SongSelectSongEntry songEntry
            && songEntry.SongMeta != currentPreviewSongMeta)
        {
            StartSongPreview(songRouletteControl.Selection.Value);
        }
    }

    public void StartSongPreview(SongSelectEntrySelection songSelectEntrySelection)
    {
        if (songSelectEntrySelection.Index != initialSongIndex)
        {
            isFirstSelectedSong = false;
        }

        if (isFirstSelectedSong
            && !songSelectSceneControl.HasPartyModeSceneData)
        {
            return;
        }

        if (songSelectEntrySelection.Entry is not SongSelectSongEntry selectedSongEntry)
        {
            return;
        }

        currentSongSelectEntryControl = songRouletteControl.EntryControls
            .FirstOrDefault(it => it.SongSelectEntry == selectedSongEntry);

        StartSongPreview(selectedSongEntry.SongMeta);
    }

    public override void StartSongPreview(SongMeta songMeta)
    {
        if (currentSongSelectEntryControl == null)
        {
            return;
        }
        songPreviewVideoImage.HideByDisplay();
        songPreviewBackgroundImage.HideByDisplay();

        base.StartSongPreview(songMeta);
    }

    protected override async Awaitable StartAudioPreviewAsync(SongMeta songMeta, int previewStartInMillis)
    {
        if (currentSongSelectEntryControl == null)
        {
            return;
        }
        await base.StartAudioPreviewAsync(songMeta, previewStartInMillis);
    }

    protected override async Awaitable StartVideoPreviewAsync(SongMeta songMeta)
    {
        if (currentSongSelectEntryControl == null)
        {
            return;
        }
        await base.StartVideoPreviewAsync(songMeta);
    }
}
