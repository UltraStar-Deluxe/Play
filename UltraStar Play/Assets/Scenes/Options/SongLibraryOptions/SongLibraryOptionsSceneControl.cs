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
    public VisualTreeAsset songIssueSongEntryUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

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
    private readonly List<SongFolderListEntryControl> songFolderListEntryControls = new();

    private void Start()
    {
        if (SongMetaManager.IsSongScanFinished)
        {
            UpdateSongIssues();
        }
        songMetaManager.ScanFilesIfNotDoneYet();
        songMetaManager.SongScanFinishedEventStream
            .Subscribe(_ => Scheduler.MainThread.Schedule(() => UpdateSongIssues()));

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
        songIssueIcon.RemoveFromClassList("error");
        songIssueIcon.RemoveFromClassList("warning");
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

        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo) },
        };
        helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_title),
            titleToContentMap,
            CloseHelp);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            () => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddAndCreateSongs)));
    }

    private void CloseHelp()
    {
        if (helpDialogControl == null)
        {
            return;
        }
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
                    // Add label for song
                    VisualElement visualElement = songIssueSongEntryUi.CloneTree().Children().First();
                    visualElement.Q<Label>(R.UxmlNames.title).text = songMetaArtistAndTitle;
                    Button openFolderButtonOfSongMeta = visualElement.Q<Button>(R.UxmlNames.openFolderButton);
                    if (PlatformUtils.IsStandalone
                        && !songIssue.SongMeta.Directory.IsNullOrEmpty()
                        && Directory.Exists(songIssue.SongMeta.Directory))
                    {
                        openFolderButtonOfSongMeta.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(songIssue.SongMeta.Directory));
                    }
                    else
                    {
                        openFolderButtonOfSongMeta.HideByDisplay();
                    }
                    accordionItemControl.AddVisualElement(visualElement);
                }

                Label songIssueLabel = new($"• {songIssue.Message}");
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

        AccordionItemControl errorsAccordionItemControl = uiManager.CreateAccordionItemControl();
        errorsAccordionItemControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_errors);
        songIssueDialogControl.AddVisualElement(errorsAccordionItemControl.VisualElement);
        FillWithSongIssues(errorsAccordionItemControl, songMetaManager.GetSongErrors());

        AccordionItemControl warningsAccordionItemControl = uiManager.CreateAccordionItemControl();
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

        songIssueDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.refresh), () =>
        {
            songMetaManager.ReloadSongMetas();
            CloseSongIssues();
        });
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

    // This method is only used on Android
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
        songFolderListEntryControls.Clear();
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            Label noSongsFoundLabel = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_noSongFoldersFoundInfo));
            noSongsFoundLabel.AddToClassList("centerHorizontalByMargin");
            noSongsFoundLabel.style.marginTop = 10;
            noSongsFoundLabel.style.marginBottom = 5;
            songList.Add(noSongsFoundLabel);

            Button viewMoreButton = new();
            viewMoreButton.AddToClassList("centerHorizontalByMargin");
            viewMoreButton.text = TranslationManager.GetTranslation(R.Messages.viewMore);
            viewMoreButton.RegisterCallbackButtonTriggered(() => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddAndCreateSongs)));
            songList.Add(viewMoreButton);
        }
        else
        {
            int index = 0;
            settings.GameSettings.songDirs.ForEach(songDir =>
            {
                CreateSongFolderEntryControl(songDir, index);
                index++;
            });
        }

        UpdateAddAndroidSongFoldersButtons();
    }

    private void CreateSongFolderEntryControl(string path, int indexInList)
    {
        VisualElement visualElement = songFolderListEntryAsset.CloneTree();
        SongFolderListEntryControl songFolderListEntryControl = new(path);
        injector
            .WithRootVisualElement(visualElement)
            .Inject(songFolderListEntryControl);

        songFolderListEntryControl.TextField.RegisterValueChangedCallback(evt =>
        {
            settings.GameSettings.songDirs[indexInList] = evt.newValue;
            UpdateAddAndroidSongFoldersButtons();

            songFolderListEntryControls.ForEach(control => control.CheckPathIsValid());
        });
        songFolderListEntryControl.DeleteButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.GameSettings.songDirs.RemoveAt(indexInList);
            UpdateSongFolderList();
            backButton.Focus();
        });

        songFolderListEntryControls.Add(songFolderListEntryControl);
        songList.Add(visualElement);
    }

    private void OnDestroy()
    {
        // Remove duplicate song folders
        settings.GameSettings.songDirs = settings.GameSettings.songDirs
            .Distinct()
            .ToList();
    }
}
