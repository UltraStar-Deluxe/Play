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

public class SongLibraryOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
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

    [Inject(UxmlName = R.UxmlNames.songList)]
    private ScrollView songList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintContainer)]
    private VisualElement androidSongFolderHintContainer;

    [Inject(UxmlName = R.UxmlNames.androidSongFolderHintLabel)]
    private Label androidSongFolderHintLabel;
    
    [Inject(UxmlName = R.UxmlNames.issuesIcon)]
    private VisualElement issuesIcon;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly List<SongFolderListEntryControl> songFolderListEntryControls = new();

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

        addButton.RegisterCallbackButtonTriggered(() => AddNewSongFolder());

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
            () => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddAndCreateSongs)));
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
                string songMetaPath = SongMetaUtils.GetAbsoluteSongMetaPath(songIssue.SongMeta);
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
                        openFolderButtonOfSongMeta.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(songIssue.SongMeta.Directory));
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
        errorsAccordionItem.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        accordionGroup.Add(errorsAccordionItem);
        FillWithSongIssues(errorsAccordionItem, songMetaManager.GetSongErrors());

        AccordionItem warningsAccordionItem = new(TranslationManager.GetTranslation(R.Messages.options_songLibrary_songIssueDialog_warnings));
        warningsAccordionItem.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
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

        issuesDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.refresh), () =>
        {
            songMetaManager.ReloadSongMetas();
            issuesDialogControl.CloseDialog();
        });
        Button closeDialogButton = issuesDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            () => issuesDialogControl.CloseDialog());
        closeDialogButton.Focus();

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
    }

    private void CreateSongFolderEntryControl(string path, int indexInList)
    {
        VisualElement visualElement = songFolderListEntryAsset.CloneTree();
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
        songList.Add(visualElement);
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
    }
}
