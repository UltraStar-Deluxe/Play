using System;
using System.IO;
using System.Linq;
using SFB;
using UniInject;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VideoAreaControl : INeedInjection, IInjectionFinishedListener, IDragListener<GeneralDragEvent>, IDisposable
{
    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SetVideoGapAction setVideoGapAction;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Injector injector;
    
    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject(UxmlName = R.UxmlNames.videoAreaLabel)]
    private Label videoAreaLabel;

    [Inject(UxmlName = R.UxmlNames.songCoverImage)]
    private VisualElement songCoverImage;

    [Inject(UxmlName = R.UxmlNames.songBackgroundImage)]
    private VisualElement songBackgroundImage;

    [Inject(UxmlName = R.UxmlNames.noVideoImage)]
    private VisualElement noVideoImage;

    [Inject(UxmlName = R.UxmlNames.videoImage)]
    private VisualElement videoImage;

    [Inject(UxmlName = R.UxmlNames.showVideoButton)]
    private Button showVideoButton;

    [Inject(UxmlName = R.UxmlNames.showCoverButton)]
    private Button showCoverButton;

    [Inject(UxmlName = R.UxmlNames.showBackgroundButton)]
    private Button showBackgroundButton;

    private bool isCanceled;
    private float videoGapAtDragStart;

    private GeneralDragControl dragControl;
    private ContextMenuControl videoImageContextMenuControl;

    public void OnInjectionFinished()
    {
        videoAreaLabel.HideByDisplay();
        if (SongMetaUtils.VideoResourceExists(songMeta))
        {
            ShowVideoImage();
        }
        else if (SongMetaUtils.CoverResourceExists(songMeta))
        {
            ShowCoverImage();
        }
        else if (SongMetaUtils.BackgroundResourceExists(songMeta))
        {
            ShowBackgroundImage();
        }

        // Init cover and background image
        UpdateCoverAndBackgroundImage();

        // Change cover and background via file dialog
        RegisterCallbackToSetFilePath(songCoverImage, () => OpenDialogToSetCoverImage());
        RegisterCallbackToSetFilePath(songBackgroundImage, () => OpenDialogToSetBackgroundImage());

        // Change video via file dialog
        RegisterCallbackToSetFilePath(noVideoImage, () => OpenDialogToSetVideo());
        
        showVideoButton.RegisterCallbackButtonTriggered(_ => ShowVideoImage());
        showBackgroundButton.RegisterCallbackButtonTriggered(_ => ShowBackgroundImage());
        showCoverButton.RegisterCallbackButtonTriggered(_ => ShowCoverImage());

        dragControl = injector
            .WithRootVisualElement(videoImage)
            .CreateAndInject<GeneralDragControl>();
        dragControl.AddListener(this);

        videoImage.RegisterCallback<PointerEnterEvent>(evt => cursorManager.SetCursorHorizontal());
        videoImage.RegisterCallback<PointerLeaveEvent>(evt => cursorManager.SetDefaultCursor());
        
        videoImageContextMenuControl = injector
            .WithRootVisualElement(videoImage)
            .CreateAndInject<ContextMenuControl>();
        videoImageContextMenuControl.FillContextMenuAction = FillVideoImageContextMenu;
    }

    private void OpenDialogToSetCoverImage()
    {
        ExtensionFilter[] imageExtensionFilters = new ExtensionFilter[] { new ExtensionFilter("Image Files", ApplicationUtils.supportedImageFiles.ToArray()) };
        OpenDialogToSetFilePath(
            "Select Cover Image",
            songMeta.Directory,
            imageExtensionFilters,
            () => songMeta.Cover,
            newValue =>
            {
                songMeta.Cover = SongMetaUtils.GetRelativePath(songMeta, newValue);
                UpdateCoverAndBackgroundImage();
            });
    }
    
    private void OpenDialogToSetBackgroundImage()
    {
        ExtensionFilter[] imageExtensionFilters = new ExtensionFilter[] { new ExtensionFilter("Image Files", ApplicationUtils.supportedImageFiles.ToArray()) };
        OpenDialogToSetFilePath(
            "Select Background Image",
            songMeta.Directory,
            imageExtensionFilters,
            () => songMeta.Background,
            newValue =>
            {
                songMeta.Background = SongMetaUtils.GetRelativePath(songMeta, newValue);
                UpdateCoverAndBackgroundImage();
            });
    }

    private void OpenDialogToSetVideo()
    {
        ExtensionFilter[] videoExtensionFilters = new ExtensionFilter[] { new ExtensionFilter("Video Files", ApplicationUtils.supportedVideoFiles.ToArray()) };
        OpenDialogToSetFilePath(
            "Select Video",
            songMeta.Directory,
            videoExtensionFilters,
            () => songMeta.Video,
            newValue =>
            {
                songMeta.Video = SongMetaUtils.GetRelativePath(songMeta, newValue);
                UpdateVideo();
                ShowVideoImage();
            });
    }
    
    private void UpdateVideo()
    {
        songVideoPlayer.SongMeta = songMeta;
    }

    private void UpdateCoverAndBackgroundImage()
    {
        if (SongMetaUtils.BackgroundResourceExists(songMeta))
        {
            ImageManager.LoadSpriteFromUri(SongMetaUtils.GetBackgroundUri(songMeta), sprite => songBackgroundImage.style.backgroundImage = new StyleBackground(sprite));
        }
        
        if (SongMetaUtils.CoverResourceExists(songMeta))
        {
            ImageManager.LoadSpriteFromUri(SongMetaUtils.GetCoverUri(songMeta), sprite => songCoverImage.style.backgroundImage = new StyleBackground(sprite));
        }
    }

    private void RegisterCallbackToSetFilePath(VisualElement visualElement, Action callback)
    {
        if (!PlatformUtils.IsStandalone)
        {
            return;
        }
        
        visualElement.RegisterCallback<PointerDownEvent>(_ => callback());
        CursorManager.SetCursorForVisualElement(visualElement, ECursor.Hand);
    }

    private void OpenDialogToSetFilePath(string dialogTitle, string fallbackDirectory, ExtensionFilter[] extensionFilters, Func<string> getter, Action<string> setter)
    {
        string oldValue = getter();
        string directory = FileUtils.Exists(oldValue)
            ? Path.GetDirectoryName(oldValue)
            : fallbackDirectory;
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }
        string selectedPath = FileSystemDialogUtils.OpenFileDialog(dialogTitle, directory, extensionFilters);
        if (!selectedPath.IsNullOrEmpty())
        {
            setter(selectedPath);
        }
    }
    
    private void FillVideoImageContextMenu(ContextMenuPopupControl contextMenu)
    {
        contextMenu.AddButton("Reset VideoGap", () => setVideoGapAction.ExecuteAndNotify(0));
        contextMenu.AddButton("Change Video", () => OpenDialogToSetVideo());
    }

    private void ShowVideoImage()
    {
        videoImage.SetVisibleByDisplay(SongMetaUtils.VideoResourceExists(songMeta));
        noVideoImage.SetVisibleByDisplay(!SongMetaUtils.VideoResourceExists(songMeta));
        songBackgroundImage.HideByDisplay();
        songCoverImage.HideByDisplay();
    }

    private void ShowBackgroundImage()
    {
        noVideoImage.HideByDisplay();
        videoImage.HideByDisplay();
        songBackgroundImage.ShowByDisplay();
        songCoverImage.HideByDisplay();
    }

    private void ShowCoverImage()
    {
        noVideoImage.HideByDisplay();
        videoImage.HideByDisplay();
        songBackgroundImage.HideByDisplay();
        songCoverImage.ShowByDisplay();
    }

    public void CancelDrag()
    {
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        isCanceled = false;
        if (dragEvent.InputButton != (int)PointerEventData.InputButton.Left)
        {
            CancelDrag();
            return;
        }

        videoGapAtDragStart = songMeta.VideoGap;
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.Execute(GetNewVideoGap(dragEvent));
        videoAreaLabel.ShowByDisplay();
        videoAreaLabel.text = $"VideoGap: {songMeta.VideoGap}";
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        setVideoGapAction.ExecuteAndNotify(GetNewVideoGap(dragEvent));
        videoAreaLabel.HideByDisplay();
    }

    private float GetNewVideoGap(GeneralDragEvent dragEvent)
    {
        float videoGapDistance = dragEvent.ScreenCoordinateInPercent.Distance.x * 2f;

        // Round to 2 decimal places
        float newVideoGap = (float)Math.Round(videoGapAtDragStart + videoGapDistance, 2);
        return newVideoGap;
    }

    public void Dispose()
    {
        dragControl?.Dispose();
    }
}
