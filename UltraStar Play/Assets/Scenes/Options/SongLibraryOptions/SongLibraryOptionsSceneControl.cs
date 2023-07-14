using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    private static readonly string songArchiveInfoJsonUrl = "https://melodymania.org/downloads/song-archives-info.json";
    
    [InjectedInInspector]
    public VisualTreeAsset songFolderListEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset downloadSongArchiveUi;
    
    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset songIssueSongEntryUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.songFolderList)]
    private VisualElement songFolderList;

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
    
    [Inject(UxmlName = R.UxmlNames.searchAudioFilesWithoutSongMetaPicker)]
    private ItemPicker searchAudioFilesWithoutSongMetaPicker;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private OptionsOverviewSceneControl optionsOverviewSceneControl;
    
    private readonly List<SongFolderListEntryControl> songFolderListEntryControls = new();
    private readonly List<DownloadSongArchiveUiControl> downloadSongArchiveUiControls = new();

    private MessageDialogControl deleteSongFolderDialog;
    
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

        settings.ObserveEveryValueChanged(gameSettings => gameSettings.SongDirs)
            .Subscribe(onNext => UpdateSongFolderList())
            .AddTo(gameObject);

        addSongFolderButton.RegisterCallbackButtonTriggered(_ => AddNewSongFolder());
        downloadSongArchiveButton.RegisterCallbackButtonTriggered(_ => CreateDownloadSongArchiveUiControl());

        new BoolPickerControl(searchAudioFilesWithoutSongMetaPicker)
            .Bind(() => settings.SearchAudioFilesWithoutSongMeta,
                newValue => settings.SearchAudioFilesWithoutSongMeta = newValue);
        
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

        StartCoroutine(WebRequestUtils.LoadTextFromUri(songArchiveInfoJsonUrl,
            json => downloadSongArchiveUiControl.SongArchiveEntries = JsonConverter.FromJson<List<SongArchiveEntry>>(json)));

        downloadSongArchiveUiControl.IsDoneWithoutError.Subscribe(newValue =>
        {
            if (newValue)
            {
                downloadSongArchiveUiControls.Remove(downloadSongArchiveUiControl);
                
                // Add new song folder if needed
                string targetFolder = downloadSongArchiveUiControl.TargetFolder;
                if (!targetFolder.IsNullOrEmpty()
                    && !settings.SongDirs.Contains(targetFolder))
                {
                    settings.SongDirs.Add(targetFolder);
                }
                
                // Fade out the download UI, then remove it
                LeanTween
                    .value(gameObject, visualElement.resolvedStyle.opacity, 0, 1f)
                    .setOnUpdate(interpolatedValue => visualElement.style.opacity = interpolatedValue)
                    .setOnComplete(_ => UpdateSongFolderList());
            }
        });

        downloadSongArchiveUiControl.DeleteEventStream.Subscribe(_ =>
        {
            downloadSongArchiveUiControls.Remove(downloadSongArchiveUiControl);
            downloadSongArchiveUiControl.CancelDownload();
            UpdateSongFolderList();
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
        settings.SongDirs.Add(path);
        UpdateSongFolderList();

        RequestExternalStoragePermissionIfNeeded();
    }

    private void UpdateSongIssues()
    {
        bool HasIssue()
        {
            return issuesIcon.ClassListContains(R.UssClasses.errorFontColor)
                   || issuesIcon.ClassListContains(R.UssClasses.warningFontColor);
        }

        bool oldHasIssue = HasIssue();

        // Update icon style
        issuesIcon.RemoveFromClassList(R.UssClasses.warningFontColor);
        issuesIcon.RemoveFromClassList(R.UssClasses.errorFontColor);
        if (songMetaManager.GetSongErrors().Count > 0)
        {
            issuesIcon.AddToClassList(R.UssClasses.errorFontColor);
        }
        else if (songMetaManager.GetSongWarnings().Count > 0)
        {
            issuesIcon.AddToClassList(R.UssClasses.warningFontColor);
        }

        // Animate icon when there are new issues
        bool newHasIssue = HasIssue();
        if (!oldHasIssue && newHasIssue)
        {
            issuesIcon.style.scale = Vector2.zero;
            LeanTween.value(gameObject, 0, 1, 1f)
                .setOnUpdate(value =>
                {
                    issuesIcon.style.scale = new Vector2(value, value);
                })
                .setEaseSpring();
        }

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(issuesIcon);
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songFormatInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_midiSongFormatInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_midiSongFormatInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_addSongInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_createSongInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_downloadSongInfo) },
            { TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songsWithoutSingAlongDataInfo_title),
                TranslationManager.GetTranslation(R.Messages.options_songLibrary_helpDialog_songsWithoutSingAlongDataInfo) },
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
        VisualElement dialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialog);

        MessageDialogControl issuesDialogControl = injector.WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        issuesDialogControl.Title = TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_title);

        AccordionGroup accordionGroup = new();
        issuesDialogControl.AddVisualElement(accordionGroup);
        
        AccordionItem errorsAccordionItem = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_errors));
        accordionGroup.Add(errorsAccordionItem);
        FillWithSongIssues(errorsAccordionItem, songMetaManager.GetSongErrors(), out List<QuickFixAction> errorQuickFixActions);

        AccordionItem warningsAccordionItem = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_warnings));
        accordionGroup.Add(warningsAccordionItem);
        FillWithSongIssues(warningsAccordionItem, songMetaManager.GetSongWarnings(), out List<QuickFixAction> warningQuickFixActions);

        if (!songMetaManager.GetSongErrors().IsNullOrEmpty())
        {
            errorsAccordionItem.ShowAccordionContent();
        }
        else if (!songMetaManager.GetSongWarnings().IsNullOrEmpty())
        {
            warningsAccordionItem.ShowAccordionContent();
        }

        // Dialog button row
        // Quick fix all buttons
        if (!errorQuickFixActions.IsNullOrEmpty())
        {
            Button quickFixAllErrorsButton = CreateQuickFixAllButton("Auto-fix errors", errorQuickFixActions);
            issuesDialogControl.AddButton(quickFixAllErrorsButton);
        }

        if (!warningQuickFixActions.IsNullOrEmpty())
        {
            Button quickFixAllWarningsButton = CreateQuickFixAllButton("Auto-fix warnings", warningQuickFixActions);
            issuesDialogControl.AddButton(quickFixAllWarningsButton);
        }

        // Refresh button
        issuesDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.refresh), _ =>
        {
            songMetaManager.ReloadSongMetas();
            issuesDialogControl.CloseDialog();
        });

        return issuesDialogControl;
    }

    private Button CreateQuickFixAllButton(string title, List<QuickFixAction> errorQuickFixActions)
    {
        Button button = new();
        button.text = title;
        button.AddToClassList("quickFixButton");
        button.RegisterCallbackButtonTriggered(_ =>
        {
            Debug.Log($"Quick fixing {errorQuickFixActions.Count} issues");
            foreach (QuickFixAction errorQuickFixAction in errorQuickFixActions)
            {
                Debug.Log("Quick fixing issue: " + errorQuickFixAction.songIssueData);
                errorQuickFixAction.action();
            }
        });
        return button;
    }

    private void FillWithSongIssues(AccordionItem accordionItem, IReadOnlyList<SongIssue> songIssues, out List<QuickFixAction> quickFixActions)
    {
        quickFixActions = new();
        if (songIssues.IsNullOrEmpty())
        {
            accordionItem.Add(new Label(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_noIssues)));
            return;
        }

        List<SongIssue> sortedSongIssues = songIssues.ToList();
        sortedSongIssues.Sort(SongIssue.compareBySongMetaArtistAndTitle);

        string lastSongMetaPath = "";
        foreach (SongIssue songIssue in sortedSongIssues)
        {
            string songMetaPath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songIssue.SongMeta);
            if (lastSongMetaPath != songMetaPath)
            {
                if (!lastSongMetaPath.IsNullOrEmpty())
                {
                    // Add empty line
                    accordionItem.Add(new Label(""));
                }

                VisualElement songIssueListSongEntry = CreateAddSongIssueListSongEntry(songIssue);
                accordionItem.Add(songIssueListSongEntry);
            }

            VisualElement songIssueUi = CreateSongIssueListIssueEntry(songIssue);
            accordionItem.Add(songIssueUi);

            // Add quick fix buttons
            if (songIssue.SongIssueData is FormatNotSupportedSongIssueData { MediaType: FormatNotSupportedSongIssueData.EMediaType.InstrumentalAudio })
            {
                Action quickFixAction = () => ConvertInstrumentalAudioToSupportedFormat(songIssue.SongMeta);
                Button quickFixButton = CreateQuickFixButton("Convert vocals audio to ogg", quickFixAction);
                quickFixActions.Add(new QuickFixAction(songIssue.SongIssueData, quickFixAction));
                accordionItem.Add(quickFixButton);
            }

            lastSongMetaPath = songMetaPath;
        }
    }

    private VisualElement CreateAddSongIssueListSongEntry(SongIssue songIssue)
    {
        string songMetaArtistAndTitle = songIssue.SongMeta != null
            ? songIssue.SongMeta.Artist + " - " + songIssue.SongMeta.Title
            : "";

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

        return visualElement;
    }

    private VisualElement CreateSongIssueListIssueEntry(SongIssue songIssue)
    {
        VisualElement visualElement = new();

        Label label = new($"• {songIssue.Message}");
        label.AddToClassList("songIssueMessage");
        visualElement.Add(label);

        return visualElement;
    }

    private Button CreateQuickFixButton(string title, Action callback)
    {
        Button button = new();
        button.AddToClassList("quickFixButton");
        button.RegisterCallbackButtonTriggered(_ => callback());
        button.text = title;
        return button;
    }

    private void ConvertInstrumentalAudioToSupportedFormat(SongMeta songMeta)
    {
        Debug.Log($"Convert vocals audio to supported format '{songMeta.InstrumentalAudio}'");
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
        if (settings.SongDirs.IsNullOrEmpty()
            && downloadSongArchiveUiControls.IsNullOrEmpty())
        {
            Label noSongsFoundLabel = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_noSongFoldersFoundInfo));
            noSongsFoundLabel.AddToClassList("mx-auto");
            noSongsFoundLabel.style.whiteSpace = WhiteSpace.Normal;
            noSongsFoundLabel.style.marginTop = 10;
            noSongsFoundLabel.style.marginBottom = 5;
            songFolderList.Add(noSongsFoundLabel);
        }
        else
        {
            int index = 0;
            settings.SongDirs.ForEach(songDir =>
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
            settings.SongDirs[indexInList] = newValue;

            songFolderListEntryControls.ForEach(control => control.CheckPathIsValid());
        });
        songFolderListEntryControl.DeleteEventStream.Subscribe(_ => OnDeleteSongFolder(indexInList));

        songFolderListEntryControls.Add(songFolderListEntryControl);
        songFolderList.Add(visualElement);
    }

    private void OnDeleteSongFolder(int indexInList)
    {
        string songFolder = CollectionUtils.SafeGet(settings.SongDirs, indexInList, "");
        if (DirectoryUtils.Exists(songFolder))
        {
            // Ask before delete
            OpenDeleteSongFolderDialog(indexInList);
            return;
        }
        
        DoDeleteSongFolder(indexInList);
    }
    
    public void OpenDeleteSongFolderDialog(int indexInList)
    {
        if (deleteSongFolderDialog != null)
        {
            return;
        }

        deleteSongFolderDialog = uiManager.CreateDialogControl("Delete Song Folder");
        deleteSongFolderDialog.DialogClosedEventStream.Subscribe(_ => deleteSongFolderDialog = null);
        deleteSongFolderDialog.Message = $"Do you want to remove the song folder\n'{settings.SongDirs[indexInList]}'?\nNo files will be deleted.";

        deleteSongFolderDialog.AddButton(TranslationManager.GetTranslation(R.Messages.no), _ => deleteSongFolderDialog.CloseDialog());
        deleteSongFolderDialog.AddButton(TranslationManager.GetTranslation(R.Messages.yes), _ =>
        {
            deleteSongFolderDialog.CloseDialog();
            DoDeleteSongFolder(indexInList);
        });
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(deleteSongFolderDialog.DialogRootVisualElement);
    }

    private void DoDeleteSongFolder(int indexInList)
    {
        settings.SongDirs.RemoveAt(indexInList);
        UpdateSongFolderList();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        issuesIcon.RemoveFromClassList("error");
        issuesIcon.RemoveFromClassList("warning");
        
        // Remove duplicate song folders
        settings.SongDirs = settings.SongDirs
            .Distinct()
            .ToList();

        songMetaManager.ReloadSongMetas();
    }

    private class QuickFixAction
    {
        public SongIssueData songIssueData { get; private set; }
        public Action action { get; private set; }

        public QuickFixAction(SongIssueData songIssueData, Action action)
        {
            this.songIssueData = songIssueData;
            this.action = action;
        }
    }
}
