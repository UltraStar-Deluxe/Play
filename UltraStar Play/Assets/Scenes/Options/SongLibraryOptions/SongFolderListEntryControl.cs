using System;
using System.IO;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongFolderListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject(Key = "initialPath")]
    private string initialPath;

    [Inject(UxmlName = R.UxmlNames.warningContainer)]
    private VisualElement warningContainer;

    [Inject(UxmlName = R.UxmlNames.warningLabel)]
    private Label warningLabel;

    [Inject(UxmlName = R.UxmlNames.pathTextField)]
    private TextField textField;

    [Inject(UxmlName = R.UxmlNames.deleteButton)]
    private Button deleteButton;

    [Inject(UxmlName = R.UxmlNames.openFolderButton)]
    private Button openSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.driveButton)]
    private Button driveButton;

    private readonly string androidSdCardPath;
    private readonly string androidInternalStoragePath;
    private ReactiveProperty<string> androidDrivePath = new("");
    private string FullPath => androidDrivePath.Value.IsNullOrEmpty()
        ? textField.value
        : PathUtils.CombinePaths(androidDrivePath.Value, textField.value);

    private readonly Subject<string> valueChangedEventStream = new();
    public IObservable<string> ValueChangedEventStream => valueChangedEventStream;

    private readonly Subject<bool> deleteEventStream = new();
    public IObservable<bool> DeleteEventStream => deleteEventStream;

    public SongFolderListEntryControl()
    {
        if (PlatformUtils.IsAndroid)
        {
            androidInternalStoragePath = AndroidUtils.GetStorageRootPath(false);
            androidSdCardPath = AndroidUtils.GetStorageRootPath(true);
        }
        else
        {
            androidInternalStoragePath = "";
            androidSdCardPath = "";
        }
    }

    public void OnInjectionFinished()
    {
        if (PlatformUtils.IsAndroid)
        {
            if (!androidSdCardPath.IsNullOrEmpty() && initialPath.StartsWith(androidSdCardPath))
            {
                androidDrivePath.Value = androidSdCardPath;
            }
            else if (!androidInternalStoragePath.IsNullOrEmpty() && initialPath.StartsWith(androidInternalStoragePath))
            {
                androidDrivePath.Value = androidInternalStoragePath;
            }
            else
            {
                androidDrivePath.Value = "";
            }

            textField.value = initialPath.Substring(androidDrivePath.Value.Length);
            UpdateDriveButton();
            driveButton.RegisterCallbackButtonTriggered(() => ToggleAndroidDrivePath());
        }
        else
        {
            textField.value = initialPath;
            androidDrivePath.Value = "";
            driveButton.HideByDisplay();
        }

        if (PlatformUtils.IsStandalone)
        {
            openSongFolderButton.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(FullPath));
        }
        else
        {
            openSongFolderButton.HideByDisplay();
        }

        deleteButton.RegisterCallbackButtonTriggered(() => deleteEventStream.OnNext(true));
        textField.RegisterValueChangedCallback(evt =>
        {
            CheckPathIsValid();
            valueChangedEventStream.OnNext(FullPath);
        });
        androidDrivePath.Subscribe(_ => valueChangedEventStream.OnNext(FullPath));
        CheckPathIsValid();
    }

    private void UpdateDriveButton()
    {
        if (!PlatformUtils.IsAndroid)
        {
            driveButton.HideByDisplay();
            return;
        }

        driveButton.ShowByDisplay();
        if (!androidInternalStoragePath.IsNullOrEmpty()
            && androidDrivePath.Value == androidInternalStoragePath)
        {
            driveButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidInternalStorage);
        }
        else if (!androidSdCardPath.IsNullOrEmpty()
                 && androidDrivePath.Value == androidSdCardPath)
        {
            driveButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidSdCardStorage);
        }
        else
        {
            driveButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidOtherStorage);
        }
    }

    private void ToggleAndroidDrivePath()
    {
        if (androidInternalStoragePath.IsNullOrEmpty()
            || androidSdCardPath.IsNullOrEmpty())
        {
            // Cannot toggle paths.
            return;
        }

        if (!androidInternalStoragePath.IsNullOrEmpty()
            && androidDrivePath.Value == androidInternalStoragePath)
        {
            // Switch to SD Card
            androidDrivePath.Value = androidSdCardPath;
        }
        else if (!androidSdCardPath.IsNullOrEmpty()
                 && androidDrivePath.Value == androidSdCardPath)
        {
            // Switch to "other", i.e., do not hide the drive prefix.
            textField.value = androidDrivePath + textField.value;
            androidDrivePath.Value = "";
        }
        else
        {
            if (!androidInternalStoragePath.IsNullOrEmpty()
                && FullPath.StartsWith(androidInternalStoragePath))
            {
                // Switch to internal storage path
                textField.value = textField.value.Substring(androidInternalStoragePath.Length);
                androidDrivePath.Value = androidInternalStoragePath;
            }
            else if (!androidSdCardPath.IsNullOrEmpty()
                     && FullPath.StartsWith(androidSdCardPath))
            {
                // Switch to internal storage path
                textField.value = textField.value.Substring(androidSdCardPath.Length);
                androidDrivePath.Value = androidInternalStoragePath;
            }
        }
        UpdateDriveButton();
    }

    private void HideWarning()
    {
        warningContainer.HideByDisplay();
        warningLabel.text = "";
    }

    private void ShowWarning(string message)
    {
        warningContainer.ShowByDisplay();
        warningLabel.text = message;
    }

    public void CheckPathIsValid()
    {
        if (Directory.Exists(FullPath))
        {
            // Check this song folder is not already added, either directly or indirectly as subfolder.
            if (SettingsProblemHintControl.IsDuplicateFolder(FullPath, settings.GameSettings.songDirs))
            {
                ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_duplicate));
            }
            else if (SettingsProblemHintControl.IsSubfolderOfAnyOtherFolder(FullPath, settings.GameSettings.songDirs, out string parentFolder))
            {
                ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_subfolderOfOtherFolder,
                    "parentFolder", parentFolder));
            }
            else
            {
                HideWarning();
            }
        }
        else if (FullPath.IsNullOrEmpty())
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_missingValue));
        }
        else if (File.Exists(FullPath))
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_noFolder));
        }
        else
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_doesNotExist));
        }
    }
}
