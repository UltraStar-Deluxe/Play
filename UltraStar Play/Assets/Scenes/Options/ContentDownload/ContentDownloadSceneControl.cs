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

    private DownloadAndExtractSongArchiveControl downloadAndExtractSongArchiveControl;
    
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

    private void StartDownload()
    {
        if (downloadAndExtractSongArchiveControl != null
            && !downloadAndExtractSongArchiveControl.IsDone.Value)
        {
            UiManager.CreateNotification("Download still in progress");
            return;
        }
        
        downloadAndExtractSongArchiveControl = new(DownloadUrl, gameObject.transform);
        downloadAndExtractSongArchiveControl.HasError.ObserveOnMainThread()
            .Subscribe(newValue =>
            {
                if (newValue)
                {
                    SetErrorStatus();
                }
            });
        downloadAndExtractSongArchiveControl.IsDoneWithoutError.ObserveOnMainThread()
            .Subscribe(newValue =>
            {
                if (newValue)
                {
                    SetFinishedStatus();
                }
            });
        downloadAndExtractSongArchiveControl.DownloadProgressEventStream.ObserveOnMainThread()
            .Subscribe(evt => UpdateDownloadProgressText(evt));
        downloadAndExtractSongArchiveControl.ExtractProgressEventStream.ObserveOnMainThread()
            .Subscribe(evt => UpdateExtractArchiveProgressText(evt));
        downloadAndExtractSongArchiveControl.Start();
    }
    
    private void CancelDownload()
    {
        downloadAndExtractSongArchiveControl?.Cancel();
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

        if (evt.FinalDownloadSizeInBytes > 0)
        {
            // Also show download progress in percent
            statusLabel.text += $" ({Math.Round(evt.DownloadProgressInPercent):0} %)";
        }
    }

    private void UpdateExtractArchiveProgressText(ExtractArchiveControl.ExtractArchiveProgressEvent evt)
    {
        if (evt.ProgressInPercent >= 100)
        {
            SetFinishedStatus();
        }
        else
        {
            statusLabel.text = $"{Math.Round(evt.ProgressInPercent):0} %";
        }
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
