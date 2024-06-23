using System;
using System.IO;
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

    [Inject(UxmlName = R.UxmlNames.selectFolderButton)]
    private Button selectFolderButton;

    [Inject(UxmlName = R.UxmlNames.openFolderButton)]
    private Button openSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.driveButton)]
    private Button driveButton;

    [Inject(UxmlName = R.UxmlNames.songFolderEnabledToggle)]
    private SlideToggle songFolderEnabledToggle;

    [Inject(UxmlName = R.UxmlNames.songFolderInactiveOverlay)]
    private VisualElement songFolderInactiveOverlay;

    private readonly string androidSdCardPath;
    private readonly string androidInternalStoragePath;
    private readonly ReactiveProperty<string> androidDrivePath = new("");
    private string FullPath => androidDrivePath.Value.IsNullOrEmpty()
        ? textField.value
        : PathUtils.CombinePaths(androidDrivePath.Value, textField.value);

    private readonly Subject<string> valueChangedEventStream = new();
    public IObservable<string> ValueChangedEventStream => valueChangedEventStream;

    private readonly Subject<VoidEvent> deleteEventStream = new();
    public IObservable<VoidEvent> DeleteEventStream => deleteEventStream;

    private readonly Subject<bool> songFolderEnabledChangedEventStream = new();
    public IObservable<bool> SongFolderEnabledChangedEventStream => songFolderEnabledChangedEventStream;

    public bool IsSongFolderEnabled { get; private set; }
    public string SongFolderPath => FullPath;

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
            driveButton.RegisterCallbackButtonTriggered(_ => ToggleAndroidDrivePath());
        }
        else
        {
            textField.value = initialPath;
            androidDrivePath.Value = "";
            driveButton.HideByDisplay();
        }

        if (PlatformUtils.IsStandalone)
        {
            selectFolderButton.RegisterCallbackButtonTriggered(_ => OpenSelectFolderDialog());
            openSongFolderButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(FullPath));
        }
        else
        {
            selectFolderButton.HideByDisplay();
            openSongFolderButton.HideByDisplay();
        }

        deleteButton.RegisterCallbackButtonTriggered(_ => deleteEventStream.OnNext(VoidEvent.instance));
        textField.DisableParseEscapeSequences();
        textField.RegisterValueChangedCallback(evt =>
        {
            UpdateButtons();
            CheckPathIsValid();
            valueChangedEventStream.OnNext(FullPath);
        });
        androidDrivePath.Subscribe(_ => valueChangedEventStream.OnNext(FullPath));
        UpdateButtons();
        CheckPathIsValid();

        IsSongFolderEnabled = !settings.DisabledSongFolders.Contains(initialPath);
        songFolderEnabledToggle.value = IsSongFolderEnabled;
        songFolderEnabledToggle.RegisterValueChangedCallback(evt =>
        {
            IsSongFolderEnabled = evt.newValue;
            UpdateInactiveOverlay();
            songFolderEnabledChangedEventStream.OnNext(evt.newValue);
        });
        UpdateInactiveOverlay();
    }

    private void UpdateButtons()
    {
        openSongFolderButton.SetEnabled(!FullPath.IsNullOrEmpty()
                                        && DirectoryUtils.Exists(FullPath));
    }

    private void UpdateInactiveOverlay()
    {
        songFolderInactiveOverlay.SetInClassList("hidden", IsSongFolderEnabled);
    }

    private void OpenSelectFolderDialog()
    {
        string selectedFolder = FileSystemDialogUtils.OpenFolderDialog("Open song folder", FullPath);
        if (selectedFolder.IsNullOrEmpty())
        {
            return;
        }

        textField.value = selectedFolder;
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
            driveButton.SetTranslatedText(Translation.Get(R.Messages.options_songLibrary_androidInternalStorage));
        }
        else if (!androidSdCardPath.IsNullOrEmpty()
                 && androidDrivePath.Value == androidSdCardPath)
        {
            driveButton.SetTranslatedText(Translation.Get(R.Messages.options_songLibrary_androidSdCardStorage));
        }
        else
        {
            driveButton.SetTranslatedText(Translation.Get(R.Messages.options_songLibrary_androidOtherStorage));
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
        warningLabel.SetTranslatedText(Translation.Empty);
    }

    private void ShowWarning(Translation message)
    {
        warningContainer.ShowByDisplay();
        warningLabel.SetTranslatedText(message);
    }

    public void CheckPathIsValid()
    {
        if (Directory.Exists(FullPath))
        {
            // Check this song folder is not already added, either directly or indirectly as subfolder.
            if (SettingsProblemHintControl.IsDuplicateFolder(FullPath, settings.SongDirs))
            {
                ShowWarning(Translation.Get(R.Messages.options_songLibrary_songFolder_duplicate));
            }
            else if (SettingsProblemHintControl.IsSubfolderOfAnyOtherFolder(FullPath, settings.SongDirs, out string parentFolder))
            {
                ShowWarning(Translation.Get(R.Messages.options_songLibrary_songFolder_subfolderOfOtherFolder,
                    "parentFolder", parentFolder));
            }
            else
            {
                HideWarning();
            }
        }
        else if (FullPath.IsNullOrEmpty())
        {
            ShowWarning(Translation.Get(R.Messages.options_songLibrary_songFolder_missingValue));
        }
        else if (File.Exists(FullPath))
        {
            ShowWarning(Translation.Get(R.Messages.options_songLibrary_songFolder_noFolder));
        }
        else
        {
            ShowWarning(Translation.Get(R.Messages.options_songLibrary_songFolder_notFound));
        }
    }
}
