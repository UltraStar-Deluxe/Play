using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
#if UNITY_ANDROID
    using UnityEngine.Android;
#endif

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongLibraryOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection
{
    private static readonly string songArchiveInfoJsonUrl = "https://ultrastar-play.com/downloads/song-archives-info.json";

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
    private SongMediaFileConversionManager songMediaFileConversionManager;

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

    [Inject(UxmlName = R.UxmlNames.searchMidiFilesWithLyricsToggle)]
    private Toggle searchMidiFilesWithLyricsToggle;

    [Inject(UxmlName = R.UxmlNames.songDataFetchTypeChooser)]
    private Chooser songDataFetchTypeChooser;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongIssueManager songIssueManager;

    [Inject]
    private OptionsOverviewSceneControl optionsOverviewSceneControl;

    private readonly List<SongFolderListEntryControl> songFolderListEntryControls = new();
    private readonly List<DownloadSongArchiveUiControl> downloadSongArchiveUiControls = new();

    private MessageDialogControl deleteSongFolderDialog;

    private string settingsAtStart;

    protected override void Start()
    {
        base.Start();

        settingsAtStart = JsonConverter.ToJson(settings);

        if (songMetaManager.IsSongScanFinished)
        {
            UpdateSongIssues();
        }
        songMetaManager.SongScanFinishedEventStream
            .Subscribe(_ => UpdateSongIssues())
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(gameSettings => gameSettings.SongDirs)
            .Subscribe(onNext => UpdateSongFolderList())
            .AddTo(gameObject);

        addSongFolderButton.RegisterCallbackButtonTriggered(_ => AddNewSongFolder());
        downloadSongArchiveButton.RegisterCallbackButtonTriggered(_ => CreateDownloadSongArchiveUiControl());

        FieldBindingUtils.Bind(searchMidiFilesWithLyricsToggle,
            () => settings.SearchMidiFilesWithLyrics,
            newValue => settings.SearchMidiFilesWithLyrics = newValue);

        new EnumChooserControl<EFetchType>(songDataFetchTypeChooser)
            .Bind(() => settings.SongDataFetchType,
                newValue => settings.SongDataFetchType = newValue);

        UpdateTranslation();

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

        // Send web request
        UnityWebRequest webRequest = UnityWebRequest.Get(new Uri(songArchiveInfoJsonUrl));
        webRequest.SendWebRequest();
        StartCoroutine(CoroutineUtils.WebRequestCoroutine(webRequest,
            downloadHandler =>
            {
                downloadSongArchiveUiControl.SongArchiveEntries =
                    JsonConverter.FromJson<List<SongArchiveEntry>>(downloadHandler.text);
            },
            ex => Debug.LogException(ex)));

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
        if (songIssueManager.GetSongErrors().Count > 0)
        {
            issuesIcon.AddToClassList(R.UssClasses.errorFontColor);
        }
        else if (songIssueManager.GetSongWarnings().Count > 0)
        {
            issuesIcon.AddToClassList(R.UssClasses.warningFontColor);
        }

        // Animate icon when there are new issues
        bool newHasIssue = HasIssue();
        if (!oldHasIssue && newHasIssue)
        {
            AnimationUtils.HighlightIconWithBounce(gameObject, issuesIcon);
        }
    }

    public override string HelpUri => Translation.Get(R.Messages.uri_howToAddAndCreateSongs);

    public override bool HasIssuesDialog => true;
    public override MessageDialogControl CreateIssuesDialogControl()
    {
        VisualElement dialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialog);

        MessageDialogControl issuesDialogControl = injector.WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        issuesDialogControl.Title = Translation.Get(R.Messages.options_songLibrary_songIssueDialog_title);

        if (songIssueManager.IsSongIssueScanFinished)
        {
            FillIssuesDialog(issuesDialogControl);
        }
        else
        {
            // Start song issue scan if needed
            if (!songIssueManager.IsSongIssueScanStarted)
            {
                songIssueManager.ReloadSongIssues();
            }

            // Show message that song issue scan is in progress
            FillIssuesDialogWithSongIssueScanInProgressMessage(issuesDialogControl);

            // Update dialog when song issue scan finished
            songIssueManager.SongIssueScanFinishedEventStream
                .SubscribeOneShot(evt =>
                {
                    issuesDialogControl.CloseDialog();
                    CreateIssuesDialogControl();
                });
        }

        return issuesDialogControl;
    }

    private void FillIssuesDialogWithSongIssueScanInProgressMessage(MessageDialogControl issuesDialogControl)
    {
        issuesDialogControl.AddVisualElement(new Label("Searching issues in loaded songs. Please wait..."));
    }

    private void FillIssuesDialog(MessageDialogControl issuesDialogControl)
    {
        AccordionGroup accordionGroup = new();
        issuesDialogControl.AddVisualElement(accordionGroup);

        AccordionItem errorsAccordionItem = new(Translation.Get(R.Messages.options_songLibrary_songIssueDialog_errors));
        accordionGroup.Add(errorsAccordionItem);
        FillWithSongIssues(errorsAccordionItem, songIssueManager.GetSongErrors(), out List<QuickFixAction> errorQuickFixActions);

        AccordionItem warningsAccordionItem = new(Translation.Get(R.Messages.options_songLibrary_songIssueDialog_warnings));
        accordionGroup.Add(warningsAccordionItem);
        FillWithSongIssues(warningsAccordionItem, songIssueManager.GetSongWarnings(), out List<QuickFixAction> warningQuickFixActions);

        if (!songIssueManager.GetSongErrors().IsNullOrEmpty())
        {
            errorsAccordionItem.ShowAccordionContent();
        }
        else if (!songIssueManager.GetSongWarnings().IsNullOrEmpty())
        {
            warningsAccordionItem.ShowAccordionContent();
        }

        // Dialog button row
        // Quick fix all buttons
        if (!errorQuickFixActions.IsNullOrEmpty())
        {
            Button quickFixAllErrorsButton = CreateQuickFixAllButton(Translation.Get(R.Messages.options_songLibrary_action_quickFixSongIssueErrors), errorQuickFixActions);
            issuesDialogControl.AddButton(quickFixAllErrorsButton);
        }

        if (!warningQuickFixActions.IsNullOrEmpty())
        {
            Button quickFixAllWarningsButton = CreateQuickFixAllButton(Translation.Get(R.Messages.options_songLibrary_action_quickFixSongIssueWarnings), warningQuickFixActions);
            issuesDialogControl.AddButton(quickFixAllWarningsButton);
        }

        // Refresh button
        issuesDialogControl.AddButton(Translation.Get(R.Messages.options_songLibrary_refreshIssues), _ =>
        {
            songMetaManager.RescanSongs();
            songIssueManager.ReloadSongIssues();

            // Update dialog
            issuesDialogControl.CloseDialog();
            CreateIssuesDialogControl();
        });
    }

    private Button CreateQuickFixAllButton(Translation title, List<QuickFixAction> quickFixActions)
    {
        Button button = new();
        button.SetTranslatedText(title);
        button.AddToClassList("quickFixAllButton");
        button.RegisterCallbackButtonTriggered(_ => CreateQuickFixAllDialog(title, quickFixActions));
        return button;
    }

    private void CreateQuickFixAllDialog(Translation title, List<QuickFixAction> quickFixActions)
    {
        MessageDialogControl quickFixAllDialog = uiManager.CreateDialogControl(title);

        ScrollView scrollView = new();
        scrollView.AddToClassList("child-mb-3");
        quickFixAllDialog.AddVisualElement(scrollView);

        List<Toggle> quickFixToggles = new();

        // Add "toggle all" toggle
        Toggle toggleAllSelectedToggle = new();
        scrollView.Add(toggleAllSelectedToggle);
        toggleAllSelectedToggle.value = true;
        toggleAllSelectedToggle.SetTranslatedLabel(Translation.Of(" "));

        toggleAllSelectedToggle.RegisterValueChangedCallback(evt =>
            quickFixToggles.ForEach(toggle => toggle.value = evt.newValue));

        // Add quick fix toggles
        foreach (QuickFixAction quickFixAction in quickFixActions)
        {
            Toggle quickFixToggle = new();
            scrollView.Add(quickFixToggle);
            quickFixToggles.Add(quickFixToggle);

            quickFixToggle.SetTranslatedLabel(quickFixAction.Title);
            quickFixToggle.value = true;
            quickFixToggle.userData = quickFixAction;
        }

        // Add buttons
        quickFixAllDialog.AddButton(Translation.Get(R.Messages.options_songLibrary_action_quickFixSongIssues), _ =>
        {
            List<QuickFixAction> selectedQuickFixActions = scrollView.Query<Toggle>()
                .Where(toggle => toggle.value)
                .ToList()
                .Select(toggle => toggle.userData as QuickFixAction)
                .Where(quickFixAction => quickFixAction != null)
                .ToList();

            Debug.Log($"Quick fixing {selectedQuickFixActions.Count} issues");
            foreach (QuickFixAction selectedQuickFixAction in selectedQuickFixActions)
            {
                Debug.Log("Quick fixing issue: " + selectedQuickFixAction.SongIssueData);
                selectedQuickFixAction.Action();
            }

            quickFixAllDialog.CloseDialog();
        });

        quickFixAllDialog.AddButton(Translation.Get(R.Messages.action_cancel),
            _ => quickFixAllDialog.CloseDialog());
    }

    private void FillWithSongIssues(AccordionItem accordionItem, IReadOnlyList<SongIssue> songIssues, out List<QuickFixAction> quickFixActions)
    {
        quickFixActions = new();
        if (songIssues.IsNullOrEmpty())
        {
            accordionItem.Add(new Label(Translation.Get(R.Messages.options_songLibrary_songIssueDialog_noIssues)));
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
            AddQuickFixButtons(accordionItem, songIssue, quickFixActions);

            lastSongMetaPath = songMetaPath;
        }
    }

    private void AddQuickFixButtons(
        VisualElement parent,
        SongIssue songIssue,
        List<QuickFixAction> quickFixActions)
    {
        if (songIssue.SongIssueData is FormatNotSupportedSongIssueData formatNotSupportedSongIssueData)
        {
            if (formatNotSupportedSongIssueData.MediaType == FormatNotSupportedSongIssueData.EMediaType.InstrumentalAudio)
            {
                Action quickFixAction = () => songMediaFileConversionManager.ConvertInstrumentalAudioToSupportedFormat(songIssue.SongMeta);
                Button quickFixButton = CreateQuickFixButton(Translation.Get(R.Messages.options_songLibrary_action_quickFix_instrumentalAudioFormat), quickFixAction);
                quickFixActions.Add(new QuickFixAction(songIssue.SongIssueData,
                    Translation.Get(R.Messages.options_songLibrary_action_quickFix_instrumentalAudioFormat),
                    quickFixAction));
                parent.Add(quickFixButton);
            }
            else if (formatNotSupportedSongIssueData.MediaType == FormatNotSupportedSongIssueData.EMediaType.VocalsAudio)
            {
                Action quickFixAction = () => songMediaFileConversionManager.ConvertVocalsAudioToSupportedFormat(songIssue.SongMeta);
                Button quickFixButton = CreateQuickFixButton(Translation.Get(R.Messages.options_songLibrary_action_quickFix_vocalsAudioFormat), quickFixAction);
                quickFixActions.Add(new QuickFixAction(songIssue.SongIssueData,
                    Translation.Get(R.Messages.options_songLibrary_action_quickFix_vocalsAudioFormat),
                    quickFixAction));
                parent.Add(quickFixButton);
            }
            else if (formatNotSupportedSongIssueData.MediaType == FormatNotSupportedSongIssueData.EMediaType.Audio)
            {
                Action quickFixAction = () => songMediaFileConversionManager.ConvertAudioToSupportedFormat(songIssue.SongMeta);
                Button quickFixButton = CreateQuickFixButton(Translation.Get(R.Messages.options_songLibrary_action_quickFix_audioFormat), quickFixAction);
                quickFixActions.Add(new QuickFixAction(songIssue.SongIssueData,
                    Translation.Get(R.Messages.options_songLibrary_action_quickFix_audioFormat),
                    quickFixAction));
                parent.Add(quickFixButton);
            }
            else if (formatNotSupportedSongIssueData.MediaType == FormatNotSupportedSongIssueData.EMediaType.Video)
            {
                Action quickFixAction = () => songMediaFileConversionManager.ConvertVideoToSupportedFormat(songIssue.SongMeta);
                Button quickFixButton = CreateQuickFixButton(Translation.Get(R.Messages.options_songLibrary_action_quickFix_videoFormat), quickFixAction);
                quickFixActions.Add(new QuickFixAction(songIssue.SongIssueData,
                    Translation.Get(R.Messages.options_songLibrary_action_quickFix_videoFormat),
                    quickFixAction));
                parent.Add(quickFixButton);
            }
        }
    }

    private VisualElement CreateAddSongIssueListSongEntry(SongIssue songIssue)
    {
        string songMetaArtistAndTitle = songIssue.SongMeta != null
            ? songIssue.SongMeta.Artist + " - " + songIssue.SongMeta.Title
            : "";

        // Add label for song
        VisualElement visualElement = songIssueSongEntryUi.CloneTree().Children().First();
        visualElement.Q<Label>(R.UxmlNames.title).SetTranslatedText(Translation.Of(songMetaArtistAndTitle));
        Button openFolderButtonOfSongMeta = visualElement.Q<Button>(R.UxmlNames.openFolderButton);
        if (PlatformUtils.IsStandalone
            && DirectoryUtils.Exists(SongMetaUtils.GetDirectoryPath(songIssue.SongMeta)))
        {
            openFolderButtonOfSongMeta.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(SongMetaUtils.GetDirectoryPath(songIssue.SongMeta)));
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

    private Button CreateQuickFixButton(Translation title, Action callback)
    {
        Button button = new();
        button.AddToClassList("quickFixButton");
        button.RegisterCallbackButtonTriggered(_ =>
        {
            callback();
            button.SetEnabled(false);
        });
        button.SetTranslatedText(title);
        return button;
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

    private void UpdateTranslation()
    {
        androidSongFolderHintLabel.SetTranslatedText(Translation.Get(R.Messages.options_songLibrary_androidFolderHint,
            // AppSpecificStorageRelativePath is the same for internal memory and sd card.
            "androidAppSpecificStorageRelativePath", AndroidUtils.GetAppSpecificStorageRelativePath(false)));
    }

    private void UpdateSongFolderList()
    {
        songFolderList.Clear();
        songFolderListEntryControls.Clear();
        if (settings.SongDirs.IsNullOrEmpty()
            && downloadSongArchiveUiControls.IsNullOrEmpty())
        {
            Label noSongsFoundLabel = new(Translation.Get(R.Messages.options_songLibrary_noSongFoldersFoundInfo));
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
    }

    private void CreateSongFolderEntryControl(string path, int indexInList)
    {
        VisualElement visualElement = songFolderListEntryUi.CloneTree();
        SongFolderListEntryControl songFolderListEntryControl = injector
            .WithRootVisualElement(visualElement)
            .WithBinding(new UniInjectBinding("initialPath", new ExistingInstanceProvider<string>(path)))
            .WithBinding(new UniInjectBinding("indexInList", new ExistingInstanceProvider<int>(indexInList)))
            .CreateAndInject<SongFolderListEntryControl>();

        songFolderListEntryControl.ValueChangedEventStream.Subscribe(newValue => OnSongFolderPathChanged(indexInList, newValue));
        songFolderListEntryControl.DeleteEventStream.Subscribe(_ => OnDeleteSongFolder(indexInList));
        songFolderListEntryControl.SongFolderEnabledChangedEventStream.Subscribe(_ => OnSongFolderEnabledChanged(indexInList));

        songFolderListEntryControls.Add(songFolderListEntryControl);
        songFolderList.Add(visualElement);
    }

    private void OnSongFolderPathChanged(int indexInList, string newValue)
    {
        settings.SongDirs[indexInList] = newValue;
        songFolderListEntryControls.ForEach(control => control.CheckPathIsValid());
        UpdateDisabledSongFoldersInSettings();
    }

    private void UpdateDisabledSongFoldersInSettings()
    {
        settings.DisabledSongFolders = songFolderListEntryControls
            .Where(it => !it.IsSongFolderEnabled)
            .Select(it => it.SongFolderPath)
            .ToList();
    }

    private void OnSongFolderEnabledChanged(int indexInList)
    {
        UpdateDisabledSongFoldersInSettings();
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

        UpdateDisabledSongFoldersInSettings();
    }

    public void OpenDeleteSongFolderDialog(int indexInList)
    {
        if (deleteSongFolderDialog != null)
        {
            return;
        }

        deleteSongFolderDialog = uiManager.CreateDialogControl(Translation.Get(R.Messages.options_songLibrary_action_deleteSongFolderDialog_title));
        deleteSongFolderDialog.DialogClosedEventStream.Subscribe(_ => deleteSongFolderDialog = null);
        deleteSongFolderDialog.Message = Translation.Get(R.Messages.options_songLibrary_action_deleteSongFolderDialog_message,
            "songFolder", settings.SongDirs[indexInList]);

        deleteSongFolderDialog.AddButton(Translation.Get(R.Messages.common_no), _ => deleteSongFolderDialog.CloseDialog());
        deleteSongFolderDialog.AddButton(Translation.Get(R.Messages.common_yes), _ =>
        {
            deleteSongFolderDialog.CloseDialog();
            DoDeleteSongFolder(indexInList);
        });
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

        if (settingsAtStart != JsonConverter.ToJson(settings))
        {
            Debug.Log("Reloading songs because settings changed");
            songMetaManager.RescanSongs();
        }
    }

    private class QuickFixAction
    {
        public Translation Title { get; private set; }
        public SongIssueData SongIssueData { get; private set; }
        public Action Action { get; private set; }

        public QuickFixAction(SongIssueData songIssueData, Translation title, Action action)
        {
            this.Title = title;
            this.SongIssueData = songIssueData;
            this.Action = action;
        }
    }
}
