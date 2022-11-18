using System;
using System.IO;
using ProTrans;
using UniInject;
using UnityEngine.UIElements;

public class SongFolderListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.warningContainer)]
    private VisualElement warningContainer;

    [Inject(UxmlName = R.UxmlNames.warningLabel)]
    private Label warningLabel;

    [Inject(UxmlName = R.UxmlNames.pathTextField)]
    public TextField TextField { get; private set; }

    [Inject(UxmlName = R.UxmlNames.deleteButton)]
    public Button DeleteButton { get; private set; }

    [Inject(UxmlName = R.UxmlNames.openFolderButton)]
    private Button openSongFolderButton;

    private string path;

    public SongFolderListEntryControl(string path)
    {
        this.path = path;
    }

    public void OnInjectionFinished()
    {
        TextField.value = path;
        TextField.RegisterValueChangedCallback(evt =>
        {
            path = evt.newValue;
            CheckPathIsValid();
        });

        if (PlatformUtils.IsStandalone)
        {
            openSongFolderButton.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(path));
        }
        else
        {
            openSongFolderButton.HideByDisplay();
        }

        CheckPathIsValid();
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
        if (Directory.Exists(path))
        {
            // Check this is not a subfolder of another song folder. Otherwise songs may be loaded multiple times.
            if (SettingsProblemHintControl.IsSubfolderOfAnyOtherFolder(path, settings.GameSettings.songDirs, out string parentFolder))
            {
                ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_subfolderOfOtherFolder,
                    "parentFolder", parentFolder));
            }
            else
            {
                HideWarning();
            }
        }
        else if (path.IsNullOrEmpty())
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_missingValue));
        }
        else if (File.Exists(path))
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_noFolder));
        }
        else
        {
            ShowWarning(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songFolder_doesNotExist));
        }
    }
}
