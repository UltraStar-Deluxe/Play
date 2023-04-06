using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContentDownloadSceneControl : AbstractOptionsSceneControl, INeedInjection
{
    [InjectedInInspector]
    public TextAsset songArchiveEntryTextAsset;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SettingsManager settingsManager;
    
    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UiManager uiManager;

    private MessageDialogControl urlChooserDialogControl;

    private DownloadAndExtractSongArchiveControl downloadAndExtractSongArchiveControl;
    
    private DownloadSongArchiveUiControl downloadSongArchiveUiControl;

    protected override void Start()
    {
        base.Start();

        downloadSongArchiveUiControl = injector
            .WithRootVisualElement(uiDocument.rootVisualElement)
            .CreateAndInject<DownloadSongArchiveUiControl>();
        downloadSongArchiveUiControl.SongArchiveEntries = JsonConverter.FromJson<List<SongArchiveEntry>>(songArchiveEntryTextAsset.text);
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_demoSongPackage) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_archiveDownload) },
            { TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads_title),
                TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_thirdPartyDownloads) },
        };
        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.contentDownloadScene_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToDownloadSongs)));
        return helpDialogControl;
    }
}
