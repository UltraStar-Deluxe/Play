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
    private static readonly List<string> expectedContentFolderSubfolders = new()
    {
        PlayerProfileUtils.PlayerProfileImagesFolderName,
        ThemeFolderUtils.ThemeFolderName,
        ModFolderUtils.ModsRootFolderName
    };

    private static readonly string PlayerProfileImageTag = "PlayerProfileImage";
    private static readonly string ThemeTag = "Theme";
    private static readonly string ModTag = "Mod";

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

    [Inject(UxmlName = R.UxmlNames.selectWorkshopItemFolderButton)]
    private Button selectWorkshopItemFolderButton;

    [Inject(UxmlName = R.UxmlNames.statusLabel)]
    private Label statusLabel;

    [Inject(UxmlName = R.UxmlNames.uploadProgressBar)]
    private ProgressBar uploadProgressBar;

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
        workshopItemFolderTextField.RegisterValueChangedCallback(evt => OnContentFolderChanged(evt.newValue));
        new TextFieldHintControl(workshopItemFolderTextField);
        new TextFieldHintControl(workshopItemImageTextField);
        statusLabel.SetTranslatedText(Translation.Empty);
    }

    public void PublishWorkshopItem()
    {
        ulong itemId = workshopItemChooserControl.Selection.IsNewItem
            ? 0
            : workshopItemChooserControl.Selection.SteamWorkshopItem.Id;
        string contentFolderPath = workshopItemFolderTextField.value.Trim();
        string previewImagePath = workshopItemImageTextField.value.Trim();
        string title = workshopItemTitleTextField.value.Trim();
        string description = workshopItemDescriptionTextField.value.Trim();
        List<string> tags = GetTagsFromContentFolder(contentFolderPath);

        string errorMessage = GetInputFieldsOrConnectionErrorMessage(
            contentFolderPath,
            previewImagePath,
            title);
        if (!errorMessage.IsNullOrEmpty())
        {
            Debug.LogError($"Upload failed. {errorMessage}");
            statusLabel.SetTranslatedText(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_error,
                "reason", errorMessage));
            return;
        }

        statusLabel.SetTranslatedText(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_uploading));
        ObservableUtils.RunOnNewTaskAsObservable<ulong>(async () =>
            {
                PublishResult publishResult = await steamWorkshopManager.PublishWorkshopItemAsync(
                    itemId,
                    contentFolderPath,
                    previewImagePath,
                    title,
                    description,
                    tags,
                    progress => ShowProgress((int)(progress * 100)));

                if (!publishResult.Success)
                {
                    Debug.LogError($"Steam Workshop publish result is {publishResult.Result.ToString()}");
                    throw new SteamException(Translation.Get(R.Messages.steamWorkshop_uploadDialog_exception_notSuccessful,
                        "publishResult", publishResult.Result.ToString()));
                }
                Debug.Log($"Successfully uploaded Steam Workshop Item. Result: {publishResult.Result}, FileId: {publishResult.FileId}");

                ApplicationUtils.OpenUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={publishResult.FileId}");

                ShowMessage(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_downloading));

                await steamWorkshopManager.SubscribeAndDownloadWorkshopItemAsync(publishResult.FileId);
                return publishResult.FileId;
            })
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to upload Steam Workshop item: {ex.Message}");
                ShowMessage(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_error,
                    "reason", ex.Message));
            })
            .Subscribe(newlyPublishedFileId =>
            {
                ShowMessage(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_success));

                // Update dropdown and select newly created Workshop Item
                UpdateWorkshopItemChooserEntries();

                WorkshopItemChooserEntry newlyPublishedWorkshopItemChooserEntry = workshopItemChooserControl.Items
                    .FirstOrDefault(item => !item.IsNewItem
                                            && item.SteamWorkshopItem.Id.Value == newlyPublishedFileId);
                if (newlyPublishedWorkshopItemChooserEntry != null)
                {
                    workshopItemChooserControl.Selection = newlyPublishedWorkshopItemChooserEntry;
                }
            });
    }

    private void OnContentFolderChanged(string newContentFolder)
    {
        Translation errorMessage = GetContentFolderErrorMessage(newContentFolder);
        if (errorMessage.Value.IsNullOrEmpty())
        {
            string expectedSubfoldersCsv = GetExistingContentFolderSubfolders(newContentFolder).JoinWith(", ");
            statusLabel.SetTranslatedText(Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_contentFolders,
                "names", expectedSubfoldersCsv));
            FillTextFieldWithDefaultsFromFolder(newContentFolder);
        }
        else
        {
            statusLabel.SetTranslatedText(errorMessage);
        }
    }

    private Translation GetContentFolderErrorMessage(string folder)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            return Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_contentFolderDoesNotExist);
        }

        if (GetExistingContentFolderSubfolders(folder).IsNullOrEmpty())
        {
            return Translation.Get(R.Messages.steamWorkshop_uploadDialog_status_contentFoldersNotFound,
                "names", expectedContentFolderSubfolders.JoinWith(", "));
        }
        return default;
    }

    private List<string> GetExistingContentFolderSubfolders(string contentFolder)
    {
        return expectedContentFolderSubfolders
            .Where(subfolder => DirectoryUtils.Exists(contentFolder + "/" + subfolder))
            .ToList();
    }

    private List<string> GetTagsFromContentFolder(string folder)
    {
        List<string> tags = new();
        List<string> actualSubfolders = GetExistingContentFolderSubfolders(folder);
        Dictionary<string, string> subfolderToTagName = new Dictionary<string, string>()
        {
            { PlayerProfileUtils.PlayerProfileImagesFolderName, PlayerProfileImageTag },
            { ThemeFolderUtils.ThemeFolderName, ThemeTag },
            { ModFolderUtils.ModsRootFolderName, ModTag }
        };
        foreach (string expectedSubfolder in expectedContentFolderSubfolders)
        {
            if (actualSubfolders.Contains(expectedSubfolder)
                && subfolderToTagName.TryGetValue(expectedSubfolder, out string tag))
            {
                tags.Add(tag);
            }
        }
        return tags;
    }

    private void InitWorkshopItemChooserControl()
    {
        List<WorkshopItemChooserEntry> entries = GetWorkshopItemChooserEntries();
        workshopItemChooserControl = new DropdownFieldControl<WorkshopItemChooserEntry>(
            workshopItemChooser,
            entries,
            entries[0],
            item => item.DisplayName);
        workshopItemChooserControl.SelectionAsObservable
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
        if (newValue.IsNewItem)
        {
            workshopItemFolderTextField.value = "";
            workshopItemImageTextField.value = "";
            workshopItemTitleTextField.value = "";
            workshopItemDescriptionTextField.value = "";
        }
        else
        {
            workshopItemFolderTextField.value = newValue.SteamWorkshopItem.Directory;
            workshopItemTitleTextField.value = newValue.SteamWorkshopItem.Title;
            workshopItemDescriptionTextField.value = newValue.SteamWorkshopItem.Description;
        }
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

        if (workshopItemChooserControl.Selection.IsNewItem)
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

    private void ShowProgress(int progressZeroToHundred)
    {
        ThreadUtils.RunOnMainThread(() => uploadProgressBar.value = progressZeroToHundred);
    }

    private void ShowMessage(Translation message)
    {
        ThreadUtils.RunOnMainThread(() => statusLabel.SetTranslatedText(message));
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
        SetValueIfEmpty(workshopItemTitleTextField, StringUtils.ToTitleCase(PathUtils.GetFileName(folder)));

        List<string> imageFiles = FileScannerUtils.ScanForFiles(
            new List<string>() { folder },
            ApplicationUtils.supportedImageFiles.Select(extension => $"*.{extension}").ToList());
        string previewImagePath = imageFiles
            .FirstOrDefault(imageFile => PathUtils.GetFileName(imageFile).Contains("preview", StringComparison.InvariantCultureIgnoreCase))
            .OrIfNull(imageFiles.FirstOrDefault());
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
