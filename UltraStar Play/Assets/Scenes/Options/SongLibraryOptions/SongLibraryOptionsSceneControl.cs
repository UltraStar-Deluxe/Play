using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset songFolderListEntryAsset;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset accordionUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.downloadSceneButton)]
    private Button downloadSceneButton;

    [Inject(UxmlName = R.UxmlNames.songList)]
    private ScrollView songList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintContainer)]
    private VisualElement androidSongFolderHintContainer;

    [Inject(UxmlName = R.UxmlNames.helpButton)]
    private Button helpButton;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintLabel)]
    private Label androidSongFolderHintLabel;

    [Inject(UxmlName = R.UxmlNames.addAndroidSdCardSongFolderButton)]
    private Button addAndroidSdCardSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.addAndroidInternalSongFolderButton)]
    private Button addAndroidInternalSongFolderButton;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private FocusableNavigator focusableNavigator;

    [Inject(UxmlName = R.UxmlNames.songIssueButton)]
    private Button songIssueButton;

    [Inject(UxmlName = R.UxmlNames.songIssueIcon)]
    private VisualElement songIssueIcon;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    private MessageDialogControl helpDialogControl;
    private MessageDialogControl songIssueDialogControl;

    private void Start()
    {
        if (SongMetaManager.IsSongScanFinished)
        {
            UpdateSongIssues();
        }
        else
        {
            songMetaManager.ScanFilesIfNotDoneYet();
            songMetaManager.SongScanFinishedEventStream
                .Subscribe(_ => Scheduler.MainThread.Schedule(() => UpdateSongIssues()));
        }

        settings.GameSettings.ObserveEveryValueChanged(gameSettings => gameSettings.songDirs)
            .Subscribe(onNext => UpdateSongFolderList())
            .AddTo(gameObject);

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.GameSettings.songDirs.Add("");
            UpdateSongFolderList();

            RequestExternalStoragePermissionIfNeeded();
        });

        downloadSceneButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ContentDownloadScene));

        backButton.RegisterCallbackButtonTriggered(() => OnBack());
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack());

        // Custom navigation targets
        focusableNavigator.AddCustomNavigationTarget(backButton, Vector2.left, helpButton, true);
        focusableNavigator.AddCustomNavigationTarget(helpButton, Vector2.left, songIssueButton, true);
        focusableNavigator.AddCustomNavigationTarget(songIssueButton, Vector2.left, downloadSceneButton, true);

        helpButton.RegisterCallbackButtonTriggered(() => ShowHelp());
        songIssueButton.RegisterCallbackButtonTriggered(() => ShowSongIssues());

#if UNITY_ANDROID
        if (AndroidUtils.GetAppSpecificStorageAbsolutePath(false).IsNullOrEmpty()
            && AndroidUtils.GetAppSpecificStorageAbsolutePath(true).IsNullOrEmpty())
        {
            // No storage folders found. Do not show any hint.
            androidSongFolderHintContainer.HideByDisplay();
        }
        else
        {
            androidSongFolderHintContainer.ShowByDisplay();
            addAndroidInternalSongFolderButton.RegisterCallbackButtonTriggered(() =>
                AddSongFolderIfNotContains(AndroidUtils.GetAppSpecificStorageAbsolutePath(false)));
            addAndroidSdCardSongFolderButton.RegisterCallbackButtonTriggered(() =>
                AddSongFolderIfNotContains(AndroidUtils.GetAppSpecificStorageAbsolutePath(true)));
        }
        UpdateAddAndroidSongFoldersButtons();
#else
        androidSongFolderHintContainer.HideByDisplay();
#endif
    }

    private void UpdateSongIssues()
    {
        // Update icon
        if (songMetaManager.GetSongErrors().Count > 0)
        {
            songIssueIcon.AddToClassList("error");
        }
        else if (songMetaManager.GetSongWarnings().Count > 0)
        {
            songIssueIcon.AddToClassList("warning");
        }
    }

    private void OnBack()
    {
        if (helpDialogControl != null)
        {
            CloseHelp();
        }
        else if (songIssueDialogControl != null)
        {
            CloseSongIssues();
        }
        else
        {
            sceneNavigator.LoadScene(EScene.OptionsScene);
        }
    }

    private void ShowHelp()
    {
        if (helpDialogControl != null)
        {
            return;
        }

        VisualElement helpDialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(helpDialog);

        helpDialogControl = injector
            .WithRootVisualElement(helpDialog)
            .CreateAndInject<MessageDialogControl>();
        helpDialogControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_title);

        AccordionItemControl songFormatInfoAccordionItemControl = CreateAccordionItemControl();
        songFormatInfoAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo_title);
        songFormatInfoAccordionItemControl.AddVisualElement(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo)));
        helpDialogControl.AddVisualElement(songFormatInfoAccordionItemControl.VisualElement);

        AccordionItemControl addSongInfoAccordionItemControl = CreateAccordionItemControl();
        addSongInfoAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo_title);
        addSongInfoAccordionItemControl.AddVisualElement(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo)));
        helpDialogControl.AddVisualElement(addSongInfoAccordionItemControl.VisualElement);

        AccordionItemControl createSongInfoAccordionItemControl = CreateAccordionItemControl();
        createSongInfoAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo_title);
        createSongInfoAccordionItemControl.AddVisualElement(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo)));
        helpDialogControl.AddVisualElement(createSongInfoAccordionItemControl.VisualElement);

        AccordionItemControl downloadSongInfoAccordionItemControl = CreateAccordionItemControl();
        downloadSongInfoAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo_title);
        downloadSongInfoAccordionItemControl.AddVisualElement(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo)));
        helpDialogControl.AddVisualElement(downloadSongInfoAccordionItemControl.VisualElement);

        Button closeDialogButton = helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            () => CloseHelp());
        closeDialogButton.Focus();
    }

    private void CloseHelp()
    {
        helpDialogControl.CloseDialog();
        helpDialogControl = null;
        helpButton.Focus();
    }

    private void ShowSongIssues()
    {
        if (songIssueDialogControl != null)
        {
            return;
        }

        void FillWithSongIssues(AccordionItemControl accordionItemControl, IReadOnlyList<SongIssue> songIssues)
        {
            if (songIssues.IsNullOrEmpty())
            {
                accordionItemControl.AddVisualElement(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_noIssues)));
                return;
            }

            List<SongIssue> sortedSongIssues = songIssues.ToList();
            sortedSongIssues.Sort(SongIssue.compareBySongMetaArtistAndTitle);

            string lastSongMetaPath = "";
            sortedSongIssues.ForEach(songIssue =>
            {
                string songMetaArtistAndTitle = songIssue.SongMeta != null
                    ? songIssue.SongMeta.Artist + " - " + songIssue.SongMeta.Title
                    : "";
                string songMetaPath = SongMetaUtils.GetAbsoluteSongMetaPath(songIssue.SongMeta);
                if (lastSongMetaPath != songMetaPath)
                {
                    if (!lastSongMetaPath.IsNullOrEmpty())
                    {
                        // Add empty line
                        accordionItemControl.AddVisualElement(new Label(""));
                    }
                    Label songMetaPathLabel = new(songMetaArtistAndTitle);
                    songMetaPathLabel.AddToClassList("songIssueSongMetaTitle");
                    accordionItemControl.AddVisualElement(songMetaPathLabel);
                }

                string messageWithBackslashReplaced = songIssue.Message.Replace("\\", "|");
                Label songIssueLabel = new($"• {messageWithBackslashReplaced}");
                songIssueLabel.AddToClassList("songIssueMessage");
                accordionItemControl.AddVisualElement(songIssueLabel);
                lastSongMetaPath = songMetaPath;
            });
        }

        VisualElement dialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialog);

        songIssueDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        songIssueDialogControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_title);

        AccordionItemControl errorsAccordionItemControl = CreateAccordionItemControl();
        errorsAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_errors);
        songIssueDialogControl.AddVisualElement(errorsAccordionItemControl.VisualElement);
        FillWithSongIssues(errorsAccordionItemControl, songMetaManager.GetSongErrors());

        AccordionItemControl warningsAccordionItemControl = CreateAccordionItemControl();
        warningsAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_warnings);
        songIssueDialogControl.AddVisualElement(warningsAccordionItemControl.VisualElement);
        FillWithSongIssues(warningsAccordionItemControl, songMetaManager.GetSongWarnings());

        if (songMetaManager.GetSongErrors().Count > 0)
        {
            errorsAccordionItemControl.ShowAccordionContent();
        }
        else if (songMetaManager.GetSongWarnings().Count > 0)
        {
            warningsAccordionItemControl.ShowAccordionContent();
        }

        Button closeDialogButton = songIssueDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            () => CloseSongIssues());
        closeDialogButton.Focus();
    }

    private void CloseSongIssues()
    {
        songIssueDialogControl.CloseDialog();
        songIssueDialogControl = null;
        songIssueButton.Focus();
    }

    private AccordionItemControl CreateAccordionItemControl()
    {
        VisualElement accordionItem = accordionUi.CloneTree().Children().FirstOrDefault();
        AccordionItemControl accordionItemControl = injector
            .WithRootVisualElement(accordionItem)
            .CreateAndInject<AccordionItemControl>();
        return accordionItemControl;
    }

    private void AddSongFolderIfNotContains(string basePath)
    {
        settings.GameSettings.songDirs.AddIfNotContains(basePath + "/Songs");
        UpdateSongFolderList();
    }

    private void UpdateAddAndroidSongFoldersButtons()
    {
        string sdCardSongFolder = AndroidUtils.GetAppSpecificStorageAbsolutePath(true);
        string internalSongFolder = AndroidUtils.GetAppSpecificStorageAbsolutePath(false);
        bool anySongDirInSdCardSongFolder = settings.GameSettings.songDirs
            .AnyMatch(songDir => songDir.StartsWith(sdCardSongFolder));
        bool anySongDirInInternalSongFolder = settings.GameSettings.songDirs
            .AnyMatch(songDir => songDir.StartsWith(internalSongFolder));
        addAndroidSdCardSongFolderButton.SetVisibleByDisplay(!sdCardSongFolder.IsNullOrEmpty() && !anySongDirInSdCardSongFolder);
        addAndroidInternalSongFolderButton.SetVisibleByDisplay(!internalSongFolder.IsNullOrEmpty() && !anySongDirInInternalSongFolder);
    }

    private static void RequestExternalStoragePermissionIfNeeded()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
#else
        // Nothing to do
#endif
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        downloadSceneButton.text = TranslationManager.GetTranslation(R.Messages.options_downloadSongs_button);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_title);
        androidSongFolderHintLabel.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidFolderHint,
            // AppSpecificStorageRelativePath is the same for internal memory and sd card.
            "androidAppSpecificStorageRelativePath", AndroidUtils.GetAppSpecificStorageRelativePath(false));
        addAndroidSdCardSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_addSdCardSongFolder);
        addAndroidInternalSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_addInternalSongFolder);
    }

    private void UpdateSongFolderList()
    {
        songList.Clear();
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            Label noSongsFoundLabel = new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_noSongFoldersFoundInfo));
            noSongsFoundLabel.AddToClassList("noSongsFoundLabel");
            songList.Add(noSongsFoundLabel);
        }
        else
        {
            int index = 0;
            settings.GameSettings.songDirs.ForEach(songDir =>
            {
                songList.Add(CreateSongFolderEntry(songDir, index));
                index++;
            });
        }

        UpdateAddAndroidSongFoldersButtons();
    }

    private VisualElement CreateSongFolderEntry(string songDir, int indexInList)
    {
        VisualElement result = songFolderListEntryAsset.CloneTree();
        VisualElement warningContainer = result.Q<VisualElement>(R.UxmlNames.warningContainer);
        Label warningLabel = warningContainer.Q<Label>(R.UxmlNames.warningLabel);

        void CheckFolderExists(string path)
        {
            if (Directory.Exists(path))
            {
                warningContainer.HideByDisplay();
            }
            else if (path.IsNullOrEmpty())
            {
                warningContainer.ShowByDisplay();
                warningLabel.text = "Enter the path to a song folder.";
            }
            else if (File.Exists(path))
            {
                warningContainer.ShowByDisplay();
                warningLabel.text = "Not a folder.";
            }
            else
            {
                warningContainer.ShowByDisplay();
                warningLabel.text = "Folder does not exist.";
            }
        }

        TextField textField = result.Q<TextField>(R.UxmlNames.pathTextField);
        textField.value = songDir;
        PathTextFieldControl pathTextFieldControl = new(textField);
        pathTextFieldControl.ValueChangedEventStream
            .Subscribe(newValueUnescaped =>
            {
                settings.GameSettings.songDirs[indexInList] = newValueUnescaped;
                CheckFolderExists(newValueUnescaped);
                UpdateAddAndroidSongFoldersButtons();
            });

        Button deleteButton = result.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.RegisterCallbackButtonTriggered(() =>
            {
                settings.GameSettings.songDirs.RemoveAt(indexInList);
                UpdateSongFolderList();
                backButton.Focus();
            });

        Button openSongFolderButton = result.Q<Button>(R.UxmlNames.openFolderButton);
        if (PlatformUtils.IsStandalone)
        {
            openSongFolderButton.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(songDir));
        }
        else
        {
            openSongFolderButton.HideByDisplay();
        }

        CheckFolderExists(songDir);

        return result;
    }

    private void OnDestroy()
    {
        // Remove duplicate song folders
        settings.GameSettings.songDirs = settings.GameSettings.songDirs
            .Distinct()
            .ToList();
    }
}
