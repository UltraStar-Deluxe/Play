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

    [Inject(UxmlName = R.UxmlNames.songPreviewVideoImage)]
    private VisualElement songPreviewVideoImage;

    [Inject(UxmlName = R.UxmlNames.songPreviewBackgroundImage)]
    private VisualElement songPreviewBackgroundImage;

    
    private SongEntryControl currentSongEntryControl;

    private int initialSongIndex;

    // The very first selected song should not be previewed.
    // The song is selected when opening the scene.
    private bool isFirstSelectedSong = true;

    protected override void Start()
    {
        base.Start();

        StartSongPreviewEventStream.Subscribe(_ =>
        {
            if (currentSongEntryControl == null)
            {
                return;
            }

            if (SongMetaUtils.VideoResourceExists(currentSongEntryControl.SongMeta))
            {
                songPreviewVideoImage.ShowByDisplay();
                songPreviewVideoImage.SetBackgroundImageAlpha(0);
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
        VideoFadeIn.Subscribe(newValue =>
        {
            if (currentSongEntryControl == null)
            {
                return;
            }
            songPreviewVideoImage.SetBackgroundImageAlpha(newValue);
        });
        BackgroundImageFadeIn.Subscribe(newValue =>
        {
            if (currentSongEntryControl == null)
            {
                return;
            }
            songPreviewBackgroundImage.SetBackgroundImageAlpha(newValue);
        });

        if (sceneData != null
            && sceneData.SongMeta != null)
        {
            initialSongIndex = songRouletteControl.Songs.IndexOf(sceneData.SongMeta);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (songRouletteControl.Selection.Value.SongMeta != currentPreviewSongMeta)
        {
            StartSongPreview(songRouletteControl.Selection.Value);
        }
    }

    public void StartSongPreview(SongSelection songSelection)
    {
        if (songSelection.SongIndex != initialSongIndex)
        {
            isFirstSelectedSong = false;
        }

        if (isFirstSelectedSong)
        {
            return;
        }

        currentSongEntryControl = songRouletteControl.SongEntryControls
            .FirstOrDefault(it => it.SongMeta == songSelection.SongMeta);

        StartSongPreview(songSelection.SongMeta);
    }

    public override void StartSongPreview(SongMeta songMeta)
    {
        if (currentSongEntryControl == null)
        {
            return;
        }
        base.StartSongPreview(songMeta);
    }

    protected override void StartAudioPreview(SongMeta songMeta, int previewStartInMillis)
    {
        if (currentSongEntryControl == null)
        {
            return;
        }
        base.StartAudioPreview(songMeta, previewStartInMillis);
    }

    protected override void StartVideoPreview(SongMeta songMeta)
    {
        if (currentSongEntryControl == null)
        {
            return;
        }
        base.StartVideoPreview(songMeta);
    }
}
