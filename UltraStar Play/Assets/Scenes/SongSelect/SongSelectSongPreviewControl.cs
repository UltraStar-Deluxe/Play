using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSongPreviewControl : SongPreviewControl
{
    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneData sceneData;

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
                currentSongEntryControl.SongPreviewVideoImage.ShowByDisplay();
                currentSongEntryControl.SongPreviewVideoImage.SetBackgroundImageAlpha(0);
            }

            currentSongEntryControl.SongPreviewBackgroundImage.ShowByDisplay();
            currentSongEntryControl.SongPreviewBackgroundImage.SetBackgroundImageAlpha(0);
        });
        StopSongPreviewEventStream.Subscribe(_ =>
        {
            songRouletteControl.SongEntryControls.ForEach(it => it.SongPreviewVideoImage.HideByDisplay());
            songRouletteControl.SongEntryControls.ForEach(it => it.SongPreviewBackgroundImage.HideByDisplay());
        });

        // Video / background image fade-in
        VideoFadeIn.Subscribe(newValue =>
        {
            if (currentSongEntryControl == null)
            {
                return;
            }
            currentSongEntryControl.SongPreviewVideoImage.SetBackgroundImageAlpha(newValue);
        });
        BackgroundImageFadeIn.Subscribe(newValue =>
        {
            if (currentSongEntryControl == null)
            {
                return;
            }
            currentSongEntryControl.SongPreviewBackgroundImage.SetBackgroundImageAlpha(newValue);
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
        if (songRouletteControl.IsDrag
            || songRouletteControl.IsFlickGesture)
        {
            return;
        }

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
