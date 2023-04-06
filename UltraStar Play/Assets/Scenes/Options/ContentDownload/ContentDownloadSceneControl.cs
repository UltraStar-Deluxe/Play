using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public TextAsset songArchiveEntryTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject(UxmlName = R.UxmlNames.statusLabel)]
    private Label statusLabel;

    [Inject(UxmlName = R.UxmlNames.urlLabel)]
    private Label urlLabel;

    [Inject(UxmlName = R.UxmlNames.urlTextField)]
    private TextField downloadPath;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startDownloadButton;

    [Inject(UxmlName = R.UxmlNames.cancelButton)]
    private Button cancelDownloadButton;

    [Inject(UxmlName = R.UxmlNames.urlChooserButton)]
    private Button urlChooserButton;

    [Inject]
    private SettingsManager settingsManager;
    
    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UiManager uiManager;

    private MessageDialogControl urlChooserDialogControl;

    private string DownloadUrl => downloadPath.value.Trim();

    private FileDownloadControl fileDownloadControl;
    private ExtractArchiveControl extractArchiveControl;

    private List<SongArchiveEntry> songArchiveEntries = new();

    protected override void Start()
    {
        base.Start();

        songArchiveEntries = JsonConverter.FromJson<List<SongArchiveEntry>>(songArchiveEntryTextAsset.text);

        statusLabel.text = "";
        SelectSongArchiveUrl(songArchiveEntries[0].Url);

        startDownloadButton.RegisterCallbackButtonTriggered(_ => StartDownload());
        cancelDownloadButton.RegisterCallbackButtonTriggered(_ => CancelDownload());

        urlChooserButton.RegisterCallbackButtonTriggered(_ => ShowUrlChooserDialog());
    }

    private void ShowUrlChooserDialog()
    {
        if (urlChooserDialogControl != null)
        {
            return;
        }

        VisualElement dialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialog);

        urlChooserDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        urlChooserDialogControl.Title = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_archiveUrlLabel);
        urlChooserDialogControl.DialogClosedEventStream.Subscribe(_ => urlChooserDialogControl = null);

        // Create a button in the dialog for every archive URL
        songArchiveEntries.ForEach(songArchiveEntry =>
        {
            Button songArchiveUrlButton = new();
            songArchiveUrlButton.AddToClassList("songArchiveUrlButton");
            songArchiveUrlButton.text = songArchiveEntry.Url;
            songArchiveUrlButton.RegisterCallbackButtonTriggered(_ =>
            {
                SelectSongArchiveUrl(songArchiveEntry.Url);
                urlChooserDialogControl?.CloseDialog();
            });
            songArchiveUrlButton.style.height = new StyleLength(StyleKeyword.Auto);
            urlChooserDialogControl.AddVisualElement(songArchiveUrlButton);

            Label songArchiveInfoLabel = new(songArchiveEntry.Description);
            songArchiveInfoLabel.AddToClassList("songArchiveInfoLabel");
            urlChooserDialogControl.AddVisualElement(songArchiveInfoLabel);
        });
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(dialog);
    }

    private void SelectSongArchiveUrl(string url)
    {
        downloadPath.value = url;
    }

    public void UpdateTranslation()
    {
        urlLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_archiveUrlLabel);
        startDownloadButton.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_startDownloadButton);
        cancelDownloadButton.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_cancelDownloadButton);
    }

    private string GetDownloadTargetPath(string url)
    {
        Uri uri = new(url);
        string filename = Path.GetFileName(uri.LocalPath);
        string targetPath = ApplicationManager.PersistentTempPath() + "/" + filename;
        return targetPath;
    }

    private DownloadHandler CreateDownloadHandler(string targetPath)
    {
        DownloadHandlerFile downloadHandler = new(targetPath);
        downloadHandler.removeFileOnAbort = true;
        return downloadHandler;
    }

    private void StartDownload()
    {
        string url = DownloadUrl;
        
        try
        {
            if (fileDownloadControl != null)
            {
                throw new Exception("Downloading file still in progress");
            }
            
            if (extractArchiveControl != null)
            {
                throw new Exception("Extracting archive still in progress");
            }
            
            if (DownloadUrl.IsNullOrEmpty())
            {
                throw new Exception("URL must not be empty");
            }
            
            string targetPath = GetDownloadTargetPath(url);
            UnityWebRequest webRequest = CreateDownloadRequest(url, targetPath);
            fileDownloadControl = FileDownloadControl.Create(webRequest, gameObject.transform);
            fileDownloadControl.BeforeDestroyEventStream.ObserveOnMainThread().Subscribe(_ => fileDownloadControl = null);
            fileDownloadControl.IsDoneWithoutError.ObserveOnMainThread().Subscribe(newValue =>
            {
                if (!newValue)
                {
                    return;
                }
                StartExtractArchive(targetPath);
            });
            fileDownloadControl.HasError.ObserveOnMainThread().Subscribe(_ => SetErrorStatus());
            fileDownloadControl.ProgressEventStream.ObserveOnMainThread().Subscribe(evt => UpdateDownloadProgressText(evt));
            fileDownloadControl.SendWebRequest();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification($"Download failed: {e.Message}");
        }
    }

    private void StartExtractArchive(string archivePath)
    {
        if (extractArchiveControl != null)
        {
            throw new Exception("Extracting archive still in progress");
        }
            
        if (!FileUtils.Exists(archivePath))
        {
            throw new FileNotFoundException(archivePath);
        }

        string targetFolder = ApplicationUtils.GetPersistentDataPath("Songs");
        extractArchiveControl = ExtractArchiveControl.Create(archivePath, targetFolder, gameObject.transform);
        extractArchiveControl.BeforeDestroyEventStream.ObserveOnMainThread().Subscribe(_ => extractArchiveControl = null);
        extractArchiveControl.IsDoneWithoutError.ObserveOnMainThread().Subscribe(_ => SetFinishedStatus());
        extractArchiveControl.HasError.ObserveOnMainThread().Subscribe(_ => SetErrorStatus());
        extractArchiveControl.ProgressEventStream.ObserveOnMainThread().Subscribe(evt => UpdateExtractArchiveProgressText(evt));
        extractArchiveControl.StartExtractArchive();
    }

    private void CancelDownload()
    {
        if (fileDownloadControl == null)
        {
            return;
        }

        Debug.Log("Aborting download");
        fileDownloadControl.AbortWebRequest();
        SetCanceledStatus();
    }

    private UnityWebRequest CreateDownloadRequest(string url, string targetPath)
    {
        DownloadHandler downloadHandler = CreateDownloadHandler(targetPath);
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.downloadHandler = downloadHandler;
        return webRequest;
    }

    private void SetFinishedStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_finished);
    }

    private void SetErrorStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_failed);
    }

    private void SetCanceledStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_canceled);
    }
    
    private void UpdateDownloadProgressText(FileDownloadControl.DownloadProgressEvent evt)
    {
        if (evt.FinalDownloadSizeInBytes > 0)
        {
            statusLabel.text = $"{Math.Round(evt.DownloadProgressInPercent):0} %";
        }
        else
        {
            ByteSizeUtils.TryGetHumanReadableByteSize((long)evt.DownloadedByteCount, out double size, out string unit);
            if (unit is "B" or "KB" or "MB")
            {
                // No digits after comma needed
                statusLabel.text = $"{size:0} {unit}";
            }
            else
            {
                statusLabel.text = $"{size:0.00} {unit}";
            }
        }
    }

    private void UpdateExtractArchiveProgressText(ExtractArchiveControl.ExtractArchiveProgressEvent evt)
    {
        statusLabel.text = $"{Math.Round(evt.ProgressInPercent):0} %";
    }
    
    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads) },
        };
        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToDownloadSongs)));
        return helpDialogControl;
    }
}
