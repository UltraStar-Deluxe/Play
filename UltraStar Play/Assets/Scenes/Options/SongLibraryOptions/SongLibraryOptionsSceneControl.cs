using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset songFolderListEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset downloadSongArchiveUi;
    
    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset songIssueSongEntryUi;

    [InjectedInInspector]
    public TextAsset songArchiveEntryTextAsset;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.songFolderList)]
    private ScrollView songFolderList;

    [Inject(UxmlName = R.UxmlNames.addSongFolderButton)]
    private Button addSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.downloadSongArchiveButton)]
    private Button downloadSongArchiveButton;
    
    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintContainer)]
    private VisualElement androidSongFolderHintContainer;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintLabel)]
    private Label androidSongFolderHintLabel;
    
    [Inject(UxmlName = R.UxmlNames.issuesIcon)]
    private VisualElement issuesIcon;
    
    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private OptionsOverviewSceneControl optionsOverviewSceneControl;
    
    private readonly List<SongFolderListEntryControl> songFolderListEntryControls = new();
    private readonly List<DownloadSongArchiveUiControl> downloadSongArchiveUiControls = new();
    
    protected override void Start()
    {
        base.Start();

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

        addSongFolderButton.RegisterCallbackButtonTriggered(_ => AddNewSongFolder());
        downloadSongArchiveButton.RegisterCallbackButtonTriggered(_ => CreateDownloadSongArchiveUiControl());

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
        }
#else
        androidSongFolderHintContainer.HideByDisplay();
#endif
    }

    private void CreateDownloadSongArchiveUiControl()
    {
        VisualElement visualElement = downloadSongArchiveUi.CloneTreeAndGetFirstChild();

        DownloadSongArchiveUiControl downloadSongArchiveUiControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<DownloadSongArchiveUiControl>();
        downloadSongArchiveUiControl.SongArchiveEntries = JsonConverter.FromJson<List<SongArchiveEntry>>(songArchiveEntryTextAsset.text);

        downloadSongArchiveUiControl.IsDoneWithoutError.Subscribe(newValue =>
        {
            if (newValue)
            {
                downloadSongArchiveUiControls.Remove(downloadSongArchiveUiControl);
                
                // Fade out the download UI, then remove it
                LeanTween
                    .value(gameObject, visualElement.resolvedStyle.opacity, 0, 1f)
                    .setOnUpdate(interpolatedValue => visualElement.style.opacity = interpolatedValue)
                    .setOnComplete(_ => visualElement.RemoveFromHierarchy());
            }
        });
        
        downloadSongArchiveUiControls.Add(downloadSongArchiveUiControl);

        UpdateSongFolderList();
    }

    private void AddNewSongFolder()
    {
        string path = "";
        if (PlatformUtils.IsAndroid)
        {
            path = AndroidUtils.GetAppSpecificStorageAbsolutePath(false) + "/Songs";
        }
        settings.GameSettings.songDirs.Add(path);
        UpdateSongFolderList();

        RequestExternalStoragePermissionIfNeeded();
    }

    private void UpdateSongIssues()
    {
        // Update icon
        issuesIcon.RemoveFromClassList("error");
        issuesIcon.RemoveFromClassList("warning");
        if (songMetaManager.GetSongErrors().Count > 0)
        {
            issuesIcon.AddToClassList("error");
        }
        else if (songMetaManager.GetSongWarnings().Count > 0)
        {
            issuesIcon.AddToClassList("warning");
        }
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
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
            
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads) },
        };
        if (PlatformUtils.IsAndroid)
        {
            titleToContentMap.Add(
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_androidSongFolders_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_androidSongFolders,
                    "androidAppSpecificStorageRelativePath", AndroidUtils.GetAppSpecificStorageRelativePath(false)));
        }

        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddAndCreateSongs)));
        return helpDialogControl;
    }
    
    public override bool HasIssuesDialog => true;
    public override MessageDialogControl CreateIssuesDialogControl()
    {
        void FillWithSongIssues(AccordionItem accordionItem, IReadOnlyList<SongIssue> songIssues)
        {
            if (songIssues.IsNullOrEmpty())
            {
                accordionItem.Add(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_noIssues)));
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
                string songMetaPath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songIssue.SongMeta);
                if (lastSongMetaPath != songMetaPath)
                {
                    if (!lastSongMetaPath.IsNullOrEmpty())
                    {
                        // Add empty line
                        accordionItem.Add(new Label(""));
                    }
                    // Add label for song
                    VisualElement visualElement = songIssueSongEntryUi.CloneTree().Children().First();
                    visualElement.Q<Label>(R.UxmlNames.title).text = songMetaArtistAndTitle;
                    Button openFolderButtonOfSongMeta = visualElement.Q<Button>(R.UxmlNames.openFolderButton);
                    if (PlatformUtils.IsStandalone
                        && !songIssue.SongMeta.Directory.IsNullOrEmpty()
                        && Directory.Exists(songIssue.SongMeta.Directory))
                    {
                        openFolderButtonOfSongMeta.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(songIssue.SongMeta.Directory));
                    }
                    else
                    {
                        openFolderButtonOfSongMeta.HideByDisplay();
                    }
                    accordionItem.Add(visualElement);
                }

                Label songIssueLabel = new($"• {songIssue.Message}");
                songIssueLabel.AddToClassList("songIssueMessage");
                accordionItem.Add(songIssueLabel);
                lastSongMetaPath = songMetaPath;
            });
        }

        VisualElement dialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialog);

        MessageDialogControl issuesDialogControl = injector.WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        issuesDialogControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_title);

        AccordionGroup accordionGroup = new();
        issuesDialogControl.AddVisualElement(accordionGroup);
        
        AccordionItem errorsAccordionItem = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_errors));
        accordionGroup.Add(errorsAccordionItem);
        FillWithSongIssues(errorsAccordionItem, songMetaManager.GetSongErrors());

        AccordionItem warningsAccordionItem = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_warnings));
        accordionGroup.Add(warningsAccordionItem);
        FillWithSongIssues(warningsAccordionItem, songMetaManager.GetSongWarnings());

        if (songMetaManager.GetSongErrors().Count > 0)
        {
            errorsAccordionItem.ShowAccordionContent();
        }
        else if (songMetaManager.GetSongWarnings().Count > 0)
        {
            warningsAccordionItem.ShowAccordionContent();
        }

        issuesDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.refresh), _ =>
        {
            songMetaManager.ReloadSongMetas();
            issuesDialogControl.CloseDialog();
        });

        return issuesDialogControl;
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
        androidSongFolderHintLabel.text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_androidFolderHint,
            // AppSpecificStorageRelativePath is the same for internal memory and sd card.
            "androidAppSpecificStorageRelativePath", AndroidUtils.GetAppSpecificStorageRelativePath(false));
    }

    private void UpdateSongFolderList()
    {
        songFolderList.Clear();
        songFolderListEntryControls.Clear();
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            Label noSongsFoundLabel = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_noSongFoldersFoundInfo));
            noSongsFoundLabel.AddToClassList("mx-auto");
            noSongsFoundLabel.style.whiteSpace = WhiteSpace.Normal;
            noSongsFoundLabel.style.marginTop = 10;
            noSongsFoundLabel.style.marginBottom = 5;
            songFolderList.Add(noSongsFoundLabel);

            Button downloadSongsButton = new();
            downloadSongsButton.text = "Download songs";
            downloadSongsButton.AddToClassList("mx-auto");
            downloadSongsButton.AddToClassList("songLibraryNoSongsButton");
            downloadSongsButton.RegisterCallbackButtonTriggered(_ => optionsOverviewSceneControl.LoadScene(EScene.ContentDownloadScene));
            songFolderList.Add(downloadSongsButton);
            
            Button viewMoreButton = new();
            viewMoreButton.AddToClassList("mx-auto");
            viewMoreButton.AddToClassList("songLibraryNoSongsButton");
            viewMoreButton.text = TranslationManager.GetTranslation(R.Messages.viewMore);
            viewMoreButton.RegisterCallbackButtonTriggered(_ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddAndCreateSongs)));
            songFolderList.Add(viewMoreButton);
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
        
        downloadSongArchiveUiControls.ForEach(downloadSongArchiveUiControl =>
        {
            songFolderList.Add(downloadSongArchiveUiControl.VisualElement);
        });
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(songFolderList);
    }

    private void CreateSongFolderEntryControl(string path, int indexInList)
    {
        VisualElement visualElement = songFolderListEntryUi.CloneTree();
        SongFolderListEntryControl songFolderListEntryControl = injector
            .WithRootVisualElement(visualElement)
            .WithBinding(new Binding("initialPath", new ExistingInstanceProvider<string>(path)))
            .CreateAndInject<SongFolderListEntryControl>();

        songFolderListEntryControl.ValueChangedEventStream.Subscribe(newValue =>
        {
            settings.GameSettings.songDirs[indexInList] = newValue;

            songFolderListEntryControls.ForEach(control => control.CheckPathIsValid());
        });
        songFolderListEntryControl.DeleteEventStream.Subscribe(_ =>
        {
            settings.GameSettings.songDirs.RemoveAt(indexInList);
            UpdateSongFolderList();
        });

        songFolderListEntryControls.Add(songFolderListEntryControl);
        songFolderList.Add(visualElement);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        issuesIcon.RemoveFromClassList("error");
        issuesIcon.RemoveFromClassList("warning");
        
        // Remove duplicate song folders
        settings.GameSettings.songDirs = settings.GameSettings.songDirs
            .Distinct()
            .ToList();

        songMetaManager.ReloadSongMetas();
    }
}
