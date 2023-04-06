using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DownloadSongArchiveUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }
    
    [Inject(UxmlName = R.UxmlNames.statusLabel)]
    private Label statusLabel;

    [Inject(UxmlName = R.UxmlNames.urlTextField)]
    private TextField urlTextField;

    [Inject(UxmlName = R.UxmlNames.toggleStartAndCancelButton)]
    private Button toggleStartAndCancelButton;

    [Inject(UxmlName = R.UxmlNames.startIcon)]
    private VisualElement startIcon;
    
    [Inject(UxmlName = R.UxmlNames.cancelIcon)]
    private VisualElement cancelIcon;
    
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

    [Inject]
    private GameObject gameObject;
    
    private MessageDialogControl urlChooserDialogControl;

    public string TargetFolder => downloadAndExtractSongArchiveControl != null
        ? downloadAndExtractSongArchiveControl.TargetFolder
        : "";

    private DownloadAndExtractSongArchiveControl downloadAndExtractSongArchiveControl;
    
    public ReactiveProperty<bool> IsDoneWithoutError { get; private set; } = new();
    
    private List<SongArchiveEntry> songArchiveEntries = new();
    public List<SongArchiveEntry> SongArchiveEntries
    {
        get => songArchiveEntries;
        set
        {
            songArchiveEntries = value;
            SelectSongArchiveUrl(songArchiveEntries.FirstOrDefault().Url);
        }
    }
    
    public void OnInjectionFinished()
    {
        urlTextField.value = "";
        new TextFieldHintControl(urlTextField);
        
        statusLabel.text = "Click the button to start the download";
        
        urlChooserButton.RegisterCallbackButtonTriggered(_ => ShowUrlChooserDialog());
        
        toggleStartAndCancelButton.RegisterCallbackButtonTriggered(_ => ToggleStartAndCancel());
        startIcon.ShowByDisplay();
        cancelIcon.HideByDisplay();
    }

    private void ToggleStartAndCancel()
    {
        if (downloadAndExtractSongArchiveControl == null
            || downloadAndExtractSongArchiveControl.IsDone.Value)
        {
            StartDownload();
        }
        else
        {
            CancelDownload();
        }
    }

    private void StartDownload()
    {
        if (downloadAndExtractSongArchiveControl != null
            && !downloadAndExtractSongArchiveControl.IsDone.Value)
        {
            UiManager.CreateNotification("Download still in progress");
            return;
        }
        
        startIcon.HideByDisplay();
        cancelIcon.ShowByDisplay();
        
        downloadAndExtractSongArchiveControl = new(urlTextField.value.Trim(), gameObject.transform);
        downloadAndExtractSongArchiveControl.ErrorMessage.ObserveOnMainThread()
            .Subscribe(newValue =>
            {
                if (!newValue.IsNullOrEmpty())
                {
                    SetErrorStatus(newValue);
                }
            });
        downloadAndExtractSongArchiveControl.IsDone.ObserveOnMainThread()
            .Subscribe(newValue =>
            {
                if (newValue)
                {
                    startIcon.ShowByDisplay();
                    cancelIcon.HideByDisplay();
                }
            });
        downloadAndExtractSongArchiveControl.IsDoneWithoutError.ObserveOnMainThread()
            .Subscribe(newValue =>
            {
                if (newValue)
                {
                    IsDoneWithoutError.Value = true;
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
        SetCanceledStatus();
    }

    private void ShowUrlChooserDialog()
    {
        if (urlChooserDialogControl != null)
        {
            return;
        }

        string title = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_archiveUrlLabel);
        urlChooserDialogControl = uiManager.CreateDialogControl(title);
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
    }

    private void SelectSongArchiveUrl(string url)
    {
        urlTextField.value = url;
    }

    private void SetFinishedStatus()
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_finished);
    }

    private void SetErrorStatus(string errorMessage)
    {
        statusLabel.text = TranslationManager.GetTranslation(R.Messages.contentDownloadScene_status_failed);    
        if (!errorMessage.IsNullOrEmpty())
        {
            statusLabel.text += $": {errorMessage}";
        }
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
}
