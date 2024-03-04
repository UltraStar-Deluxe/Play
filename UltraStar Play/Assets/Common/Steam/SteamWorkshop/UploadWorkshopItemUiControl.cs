using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks.Ugc;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UploadWorkshopItemUiControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject(UxmlName = R.UxmlNames.workshopItemChooser)]
    private DropdownField workshopItemChooser;

    [Inject(UxmlName = R.UxmlNames.workshopItemFolderTextField)]
    private TextField workshopItemFolderTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemTitleTextField)]
    private TextField workshopItemTitleTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemImageTextField)]
    private TextField workshopItemImageTextField;

    [Inject(UxmlName = R.UxmlNames.selectWorkshopItemImageButton)]
    private Button selectWorkshopItemImageButton;

    [Inject(UxmlName = R.UxmlNames.openWorkshopItemFolderButton)]
    private Button openWorkshopItemFolderButton;

    [Inject(UxmlName = R.UxmlNames.workshopItemDescriptionTextField)]
    private TextField workshopItemDescriptionTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemTagCsvTextField)]
    private TextField workshopItemTagCsvTextField;

    [Inject(UxmlName = R.UxmlNames.selectWorkshopItemFolderButton)]
    private Button selectWorkshopItemFolderButton;

    [Inject(UxmlName = R.UxmlNames.uploadProgressLabel)]
    private Label uploadProgressLabel;

    [Inject(UxmlName = R.UxmlNames.infoLabel)]
    private Label infoLabel;

    [Inject(UxmlName = R.UxmlNames.infoContainer)]
    private VisualElement infoContainer;

    [Inject]
    private SteamManager steamManager;

    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    private readonly List<IDisposable> disposables = new();

    private DropdownFieldControl<WorkshopItemChooserEntry> workshopItemChooserControl;

    public void OnInjectionFinished()
    {
        InitWorkshopItemChooserControl();

        selectWorkshopItemFolderButton.RegisterCallbackButtonTriggered(_ => OpenSelectFolderDialog());
        selectWorkshopItemImageButton.RegisterCallbackButtonTriggered(_ => OpenSelectPreviewImageDialog());
        openWorkshopItemFolderButton.RegisterCallbackButtonTriggered(_ => OpenWorkshopItemFolder());
        workshopItemFolderTextField.RegisterValueChangedCallback(evt => FillTextFieldWithDefaultsFromFolder(evt.newValue));
        uploadProgressLabel.text = "";
    }

    public void PublishWorkshopItem()
    {
        ulong itemId = workshopItemChooserControl.SelectedItem.IsNewItem
            ? 0
            : workshopItemChooserControl.SelectedItem.SteamWorkshopItem.Id;
        string contentFolderPath = workshopItemFolderTextField.value.Trim();
        string previewImagePath = workshopItemImageTextField.value.Trim();
        string title = workshopItemTitleTextField.value.Trim();
        string description = workshopItemDescriptionTextField.value.Trim();
        List<string> tags = workshopItemTagCsvTextField.value
            .Split(",")
            .Select(tag => tag.Trim())
            .ToList();

        string errorMessage = GetInputFieldsOrConnectionErrorMessage(
            contentFolderPath,
            previewImagePath,
            title);
        if (!errorMessage.IsNullOrEmpty())
        {
            string fullErrorMessage = $"Upload failed. {errorMessage}";
            Debug.LogError(fullErrorMessage);
            uploadProgressLabel.text = fullErrorMessage;
            return;
        }

        ulong newlyPublishedFileId = 0;

        uploadProgressLabel.text = "Uploading...";
        ObservableUtils.RunOnNewTaskAsObservable(async () => await steamWorkshopManager.PublishWorkshopItemAsync(
                itemId,
                contentFolderPath,
                previewImagePath,
                title,
                description,
                tags,
                ShowUploadProgress))
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to upload Steam Workshop item: {ex.Message}");
                ShowUploadFailure();
            })
            .SelectMany(publishResult =>
            {
                if (publishResult.Success)
                {
                    Debug.Log($"Successfully uploaded Steam Workshop Item. Result: {publishResult.Result}, FileId: {publishResult.FileId}");
                    ShowUploadSuccess();

                    ApplicationUtils.OpenUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={publishResult.FileId}");
                    newlyPublishedFileId = publishResult.FileId;

                    // Download newly created Workshop Item
                    return steamWorkshopManager.DownloadWorkshopItemsAsObservable();
                }
                else
                {
                    Debug.LogError($"Upload of Steam Workshop Item not successful. Result: {publishResult.Result}");
                    ShowUploadFailure();
                    return Observable.Empty<IReadOnlyList<Item>>();
                }
            })
            .Subscribe(_ =>
            {
                // Update dropdown and select newly created Workshop Item
                UpdateWorkshopItemChooserEntries();

                WorkshopItemChooserEntry newlyPublishedWorkshopItemChooserEntry = workshopItemChooserControl.Items
                    .FirstOrDefault(item => !item.IsNewItem
                                            && item.SteamWorkshopItem.Id.Value == newlyPublishedFileId);
                if (newlyPublishedWorkshopItemChooserEntry != null)
                {
                    workshopItemChooserControl.SetSelection(newlyPublishedWorkshopItemChooserEntry);
                }
            });
    }

    private void InitWorkshopItemChooserControl()
    {
        List<WorkshopItemChooserEntry> entries = GetWorkshopItemChooserEntries();
        workshopItemChooserControl = new DropdownFieldControl<WorkshopItemChooserEntry>(
            workshopItemChooser,
            entries,
            entries[0],
            item => item.DisplayName);
        workshopItemChooserControl.Selection
            .Subscribe(newValue => OnWorkshopItemChooserSelectionChanged(newValue));

        if (steamWorkshopManager.DownloadState is not SteamWorkshopManager.EDownloadState.Finished)
        {
            disposables.Add(steamWorkshopManager.FinishDownloadWorkshopItemsEventStream
                .Subscribe(_ => UpdateWorkshopItemChooserEntries()));
        }
    }

    private void UpdateWorkshopItemChooserEntries()
    {
        workshopItemChooserControl.Items = GetWorkshopItemChooserEntries();
    }

    private void OnWorkshopItemChooserSelectionChanged(WorkshopItemChooserEntry newValue)
    {
        UpdateMandatoryFieldsInfo(newValue.IsNewItem);
        if (newValue.IsNewItem)
        {
            workshopItemFolderTextField.value = "";
            workshopItemImageTextField.value = "";
            workshopItemTitleTextField.value = "";
            workshopItemDescriptionTextField.value = "";
            workshopItemTagCsvTextField.value = "";
        }
        else
        {
            workshopItemFolderTextField.value = newValue.SteamWorkshopItem.Directory;
            workshopItemTitleTextField.value = newValue.SteamWorkshopItem.Title;
            workshopItemDescriptionTextField.value = newValue.SteamWorkshopItem.Description;
            workshopItemTagCsvTextField.value = newValue.SteamWorkshopItem.Tags.ToCsv(", ", "", "");
        }
    }

    private void UpdateMandatoryFieldsInfo(bool isCreatingNewWorkshopItem)
    {
        infoLabel.text = isCreatingNewWorkshopItem
            ? ""
            : "Only set the values that you want to change";
        infoContainer.SetVisibleByDisplay(!infoLabel.text.IsNullOrEmpty());
    }

    private List<WorkshopItemChooserEntry> GetWorkshopItemChooserEntries()
    {
        return new List<WorkshopItemChooserEntry>() { new WorkshopItemChooserEntry() }
            .Union(steamWorkshopManager.DownloadedWorkshopItems
                .Where(item => item.Owner.Id == steamManager.PlayerSteamId)
                .Select(item => new WorkshopItemChooserEntry(item)))
            .ToList();
    }

    private string GetInputFieldsOrConnectionErrorMessage(
        string contentFolderPath,
        string imagePath,
        string title)
    {
        if (!steamManager.IsConnectedToSteam)
        {
            return "Not connected to Steam.";
        }

        if (workshopItemChooserControl.SelectedItem.IsNewItem)
        {
            // For an new item, all mandatory fields must be set.
            if (!DirectoryUtils.Exists(contentFolderPath))
            {
                return "Folder does not exist";
            }

            if (!FileUtils.Exists(imagePath))
            {
                return "Preview image does not exist";
            }

            if (title.IsNullOrEmpty())
            {
                return "Title cannot be empty";
            }
        }
        else
        {
            // For an existing item, only fields that should be changed need to be set.
            if (!contentFolderPath.IsNullOrEmpty()
                && !DirectoryUtils.Exists(contentFolderPath))
            {
                return "Folder does not exist";
            }

            if (!imagePath.IsNullOrEmpty()
                && !FileUtils.Exists(imagePath))
            {
                return "Preview image does not exist";
            }
        }

        return "";
    }

    private void ShowUploadSuccess()
    {
        ThreadUtils.RunOnMainThread(() =>
        {
            uploadProgressLabel.text = "Upload successful.";
        });
    }

    private void ShowUploadFailure()
    {
        ThreadUtils.RunOnMainThread(() =>
        {
            uploadProgressLabel.text = $"Upload failed. See log for details.";
        });
    }

    private void ShowUploadProgress(float progress)
    {
        ThreadUtils.RunOnMainThread(() =>
        {
            uploadProgressLabel.text = $"{progress:F2} %";
        });
    }

    private void OpenWorkshopItemFolder()
    {
        if (!DirectoryUtils.Exists(workshopItemFolderTextField.value))
        {
            return;
        }
        ApplicationUtils.OpenDirectory(workshopItemFolderTextField.value);
    }

    private void OpenSelectFolderDialog()
    {
        FileSystemDialogUtils.OpenFolderDialogToSetPath(
            "Open Content Folder of Workshop Item",
            ModFolderUtils.GetUserDefinedModsRootFolderAbsolutePath(),
            () => workshopItemFolderTextField.value,
            newValue => workshopItemFolderTextField.value = newValue);
    }

    private void OpenSelectPreviewImageDialog()
    {
        FileSystemDialogUtils.OpenFileDialogToSetPath(
            "Select Preview Image",
            ModFolderUtils.GetUserDefinedModsRootFolderAbsolutePath(),
            FileSystemDialogUtils.CreateExtensionFilters("Image files", ApplicationUtils.supportedImageFiles),
            () => workshopItemImageTextField.value,
            newValue => workshopItemImageTextField.value = newValue);
    }

    private void FillTextFieldWithDefaultsFromFolder(string folder)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            uploadProgressLabel.text = $"Folder does not exist.";
            return;
        }
        uploadProgressLabel.text = $"";

        SetValueIfEmpty(workshopItemTitleTextField, StringUtils.ToTitleCase(PathUtils.GetFileName(folder)));

        List<string> imageFiles = FileScannerUtils.ScanForFiles(
            new List<string>() { folder },
            ApplicationUtils.supportedImageFiles.Select(extension => $"*.{extension}").ToList());
        string previewImagePath = imageFiles.FirstOrDefault();
        if (FileUtils.Exists(previewImagePath))
        {
            SetValueIfEmpty(workshopItemImageTextField, previewImagePath);
        }
    }

    public void Dispose()
    {
        disposables.ForEach(it => it.Dispose());
    }

    private static void SetValueIfEmpty(TextField textField, string newValue)
    {
        if (textField.value.IsNullOrEmpty())
        {
            textField.value = newValue;
        }
    }

    private class WorkshopItemChooserEntry
    {
        private const string NewItemDisplayText = "New Item";

        public Item SteamWorkshopItem { get; private set; }
        public bool IsNewItem { get; private set; }
        public string DisplayName => IsNewItem ? NewItemDisplayText : SteamWorkshopItem.Title;

        public WorkshopItemChooserEntry()
        {
            IsNewItem = true;
        }

        public WorkshopItemChooserEntry(Item steamWorkshopItem)
        {
            SteamWorkshopItem = steamWorkshopItem;
            IsNewItem = false;
        }
    }
}
