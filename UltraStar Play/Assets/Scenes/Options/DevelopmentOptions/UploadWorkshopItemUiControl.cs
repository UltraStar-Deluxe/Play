using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steamworks.Ugc;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UploadWorkshopItemUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.workshopItemFolderTextField)]
    private TextField workshopItemFolderTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemTitleTextField)]
    private TextField workshopItemTitleTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemDescriptionTextField)]
    private TextField workshopItemDescriptionTextField;

    [Inject(UxmlName = R.UxmlNames.workshopItemTagCsvTextField)]
    private TextField workshopItemTagCsvTextField;

    [Inject(UxmlName = R.UxmlNames.selectWorkshopItemFolderButton)]
    private Button selectWorkshopItemFolderButton;

    [Inject(UxmlName = R.UxmlNames.uploadProgressLabel)]
    private Label uploadProgressLabel;

    [Inject]
    private SteamManager steamManager;

    public void OnInjectionFinished()
    {
        selectWorkshopItemFolderButton.RegisterCallbackButtonTriggered(_ => OpenSelectFolderDialog());
        workshopItemFolderTextField.RegisterValueChangedCallback(evt => FillTextFieldWithDefaultsFromFolder(evt.newValue));
        uploadProgressLabel.text = "";
    }

    public void PublishWorkshopItem()
    {
        string contentFolderPath = workshopItemFolderTextField.value.Trim();
        string title = workshopItemTitleTextField.value.Trim();
        string description = workshopItemDescriptionTextField.value.Trim();
        List<string> tags = workshopItemTagCsvTextField.value
            .Split(",")
            .Select(tag => tag.Trim())
            .ToList();

        string errorMessage = GetInputFieldsOrConnectionErrorMessage(
            contentFolderPath,
            title);
        if (!errorMessage.IsNullOrEmpty())
        {
            string fullErrorMessage = $"Upload failed. {errorMessage}";
            Debug.LogError(fullErrorMessage);
            uploadProgressLabel.text = fullErrorMessage;
            return;
        }

        uploadProgressLabel.text = "Uploading...";
        ObservableUtils.RunOnNewTaskAsObservable(async () => await PublishWorkshopItemAsync(
                contentFolderPath,
                title,
                description,
                tags,
                ShowUploadProgress))
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to upload Steam Workshop item: {ex.Message}");
                ShowUploadFailure();
            })
            .Subscribe(publishResult =>
            {
                if (publishResult.Success)
                {
                    Debug.Log($"Successfully uploaded Steam Workshop Item. Result: {publishResult.Result}, FileId: {publishResult.FileId}");
                    ShowUploadSuccess();
                }
                else
                {
                    Debug.LogError($"Upload of Steam Workshop Item not successful. Result: {publishResult.Result}");
                    ShowUploadFailure();
                }
            });
    }

    private string GetInputFieldsOrConnectionErrorMessage(
        string contentFolderPath,
        string title)
    {
        if (!DirectoryUtils.Exists(contentFolderPath))
        {
            return "Folder does not exist";
        }

        if (title.IsNullOrEmpty())
        {
            return "Title cannot be empty";
        }

        if (!steamManager.IsConnectedToSteam)
        {
            return "Not connected to Steam.";
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

    private async Task<PublishResult> PublishWorkshopItemAsync(
        string contentFolderPath,
        string title,
        string description,
        List<string> tags,
        Action<float> onProgress)
    {
        Editor editor = Editor.NewCommunityFile
            .ForAppId(SteamConstants.MelodyManiaSteamAppId)
            .WithPublicVisibility()
            .WithTitle(title)
            .WithDescription(description)
            .WithContent(contentFolderPath)
            .WithPreviewFile($"{contentFolderPath}/workshop-preview.jpg");
        foreach (string tag in tags)
        {
            editor.WithTag(tag);
        }

        return await editor
            .SubmitAsync(new ActionProgress(onProgress));
    }

    private void OpenSelectFolderDialog()
    {
        string selectedFolder = FileSystemDialogUtils.OpenFolderDialog("Open Content Folder of Workshop Item", ModManager.GetAbsoluteUserDefinedModsRootFolder());
        if (selectedFolder.IsNullOrEmpty())
        {
            return;
        }

        workshopItemFolderTextField.value = selectedFolder;
    }

    private void FillTextFieldWithDefaultsFromFolder(string folder)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            uploadProgressLabel.text = $"Folder does not exist.";
            return;
        }

        uploadProgressLabel.text = $"";

        List<string> ymlFiles = FileScannerUtils.ScanForFiles(
            new List<string>() { folder },
            new List<string>() { "*.yml" });
        string modInfoFilePath = ymlFiles.FirstOrDefault(path => PathUtils.GetFileName(path) == ModManager.ModInfoFileName);
        if (FileUtils.Exists(modInfoFilePath))
        {
            FillTextFieldWithDefaultsFromModInfoFile(modInfoFilePath);
            return;
        }

        workshopItemTitleTextField.value = PathUtils.GetFileName(folder);
    }

    private void FillTextFieldWithDefaultsFromModInfoFile(string modInfoFilePath)
    {
        try
        {
            string modInfoFileContent = FileUtils.ReadAllText(modInfoFilePath);
            ModInfo fromYaml = YamlConverter.FromYaml<ModInfo>(modInfoFileContent);
            workshopItemTitleTextField.value = PathUtils.GetFileName(fromYaml.name);
            workshopItemDescriptionTextField.value = PathUtils.GetFileName(fromYaml.description);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to fill text fields with content from '{modInfoFilePath}': {ex.Message}");
        }
    }

    private class ActionProgress : IProgress<float>
    {
        private readonly Action<float> onProgress;

        public ActionProgress(Action<float> onProgress)
        {
            this.onProgress = onProgress;
        }

        public void Report(float progress)
        {
            onProgress?.Invoke(progress);
        }
    }
}
